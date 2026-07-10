(() => {
    'use strict';

    document.addEventListener('submit', event => {
        const form = event.target.closest('form[data-confirm]');
        if (!form) return;

        const message = form.dataset.confirm;
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    document.addEventListener('click', event => {
        const confirmTarget = event.target.closest('[data-confirm]');
        if (confirmTarget && confirmTarget.tagName !== 'FORM') {
            const message = confirmTarget.dataset.confirm;
            if (message && !window.confirm(message)) {
                event.preventDefault();
                event.stopImmediatePropagation();
                return;
            }
        }

        if (event.target.closest('[data-print-page]')) {
            event.preventDefault();
            window.print();
        }
    });

    document.querySelectorAll('[data-progress-value]').forEach(element => {
        const rawValue = Number.parseFloat(element.dataset.progressValue ?? '0');
        const value = Number.isFinite(rawValue) ? Math.min(100, Math.max(0, rawValue)) : 0;
        element.style.width = `${value}%`;
    });
})();
