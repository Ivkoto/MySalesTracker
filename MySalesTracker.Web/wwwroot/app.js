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
    getCurrentHourKey: function () {
        const pad = function (value) { return String(value).padStart(2, "0"); };
        const now = new Date();
        return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}`;
    },

    showCurrentHour: function (scroller) {
        if (!scroller) {
            return false;
        }

        const currentHour = window.weatherForecast.getCurrentHourKey();
        const items = Array.from(scroller.querySelectorAll(".hour-item"));

        for (const item of items) {
            item.classList.remove("hour-item-current");
            item.removeAttribute("aria-current");
        }

        const currentItem = items.find(function (item) {
            return item.dataset.forecastHour === currentHour;
        });

        if (!currentItem) {
            scroller.scrollLeft = 0;
            return false;
        }

        currentItem.classList.add("hour-item-current");
        currentItem.setAttribute("aria-current", "time");
        scroller.scrollLeft += currentItem.getBoundingClientRect().left - scroller.getBoundingClientRect().left;
        return true;
    }
};

