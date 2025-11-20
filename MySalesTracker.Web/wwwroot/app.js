// Select all text in an input element when it receives focus
window.selectAllText = function (element) {
    if (element && element.select) {
        element.select();
    }
};

