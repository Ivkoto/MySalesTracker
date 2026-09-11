// Select all text in an input element when it receives focus
window.selectAllText = function (element) {
    if (element && element.select) {
        element.select();
    }
};

window.weatherLocation = {
    getCurrent: function () {
        if (!window.isSecureContext) {
            return Promise.resolve({ error: "insecure" });
        }
        if (!navigator.geolocation) {
            return Promise.resolve({ error: "unsupported" });
        }

        return new Promise(function (resolve) {
            navigator.geolocation.getCurrentPosition(
                async function (position) {
                    const latitude = position.coords.latitude;
                    const longitude = position.coords.longitude;
                    const controller = new AbortController();
                    const timeout = setTimeout(function () { controller.abort(); }, 8000);
                    try {
                        const query = new URLSearchParams({ latitude, longitude, localityLanguage: "bg" });
                        const response = await fetch("https://api.bigdatacloud.net/data/reverse-geocode-client?" + query, {
                            signal: controller.signal,
                            credentials: "omit"
                        });
                        if (!response.ok) {
                            resolve({ error: "city-unavailable" });
                            return;
                        }
                        const place = await response.json();
                        const city = place.city?.trim() || place.locality?.trim();
                        resolve(city ? { latitude, longitude, city } : { error: "city-unavailable" });
                    } catch {
                        resolve({ error: "city-unavailable" });
                    } finally {
                        clearTimeout(timeout);
                    }
                },
                function (error) {
                    resolve({ error: { 1: "denied", 2: "unavailable", 3: "timeout" }[error.code] || "unavailable" });
                },
                { enableHighAccuracy: false, timeout: 10000, maximumAge: 60000 }
            );
        });
    }
};

window.weatherForecast = {
    getCurrentHourKey: function (timeZone) {
        const pad = function (value) { return String(value).padStart(2, "0"); };
        const now = new Date();

        if (timeZone) {
            try {
                const parts = {};
                const formatter = new Intl.DateTimeFormat("en-US", {
                    timeZone,
                    year: "numeric",
                    month: "2-digit",
                    day: "2-digit",
                    hour: "2-digit",
                    hourCycle: "h23"
                });
                for (const part of formatter.formatToParts(now)) {
                    if (part.type !== "literal") {
                        parts[part.type] = part.value;
                    }
                }
                return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}`;
            } catch {
                return null;
            }
        }

        return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}`;
    },

    showCurrentHour: function (scroller, currentHour, timeZone) {
        if (!scroller) {
            return false;
        }

        if (scroller.weatherHourTimer) {
            clearTimeout(scroller.weatherHourTimer);
        }

        const positionHour = function (hourKey, shouldScroll) {
            const items = Array.from(scroller.querySelectorAll(".hour-item"));

            for (const item of items) {
                item.classList.remove("hour-item-current");
                item.removeAttribute("aria-current");
            }

            const currentItem = hourKey && items.find(function (item) {
                return item.dataset.forecastHour === hourKey;
            });

            if (!currentItem) {
                if (shouldScroll) {
                    scroller.scrollLeft = 0;
                }
                return false;
            }

            currentItem.classList.add("hour-item-current");
            currentItem.setAttribute("aria-current", "time");
            if (shouldScroll) {
                scroller.scrollLeft += currentItem.getBoundingClientRect().left - scroller.getBoundingClientRect().left;
            }
            return true;
        };

        const forecastHour = window.weatherForecast.getCurrentHourKey(timeZone) || currentHour;
        const positioned = positionHour(forecastHour, true);

        if (timeZone) {
            const refreshMarker = function () {
                if (!scroller.isConnected) {
                    return;
                }
                positionHour(window.weatherForecast.getCurrentHourKey(timeZone) || currentHour, false);
                scroller.weatherHourTimer = setTimeout(refreshMarker, 60000);
            };
            const nextMinute = 60000 - (Date.now() % 60000) + 50;
            scroller.weatherHourTimer = setTimeout(refreshMarker, nextMinute);
        }

        return positioned;
    }
};

