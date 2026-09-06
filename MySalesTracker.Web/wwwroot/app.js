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

