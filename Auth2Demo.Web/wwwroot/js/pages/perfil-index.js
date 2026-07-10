(() => {
    const form = document.querySelector('.profile-password-panel');
    if (!form) return;

    const password = form.querySelector('#newPassword');
    const confirmation = form.querySelector('#confirmPassword');
    const rules = document.querySelector('#profile-password-rules');
    if (!password || !confirmation || !rules) return;

    const requiredLength = Number(form.dataset.requiredLength || '8');
    const requirements = {
        length: value => value.length >= requiredLength,
        uppercase: value => /[A-Z]/.test(value),
        lowercase: value => /[a-z]/.test(value),
        digit: value => /[0-9]/.test(value),
        special: value => /[^a-zA-Z0-9]/.test(value),
        confirmation: value => value.length > 0 && value === confirmation.value
    };

    const enabled = {
        length: true,
        uppercase: form.dataset.requireUppercase === 'true',
        lowercase: form.dataset.requireLowercase === 'true',
        digit: form.dataset.requireDigit === 'true',
        special: form.dataset.requireSpecial === 'true',
        confirmation: true
    };

    function refreshRules() {
        const value = password.value;
        for (const [name, validate] of Object.entries(requirements)) {
            if (!enabled[name]) continue;
            const item = rules.querySelector(`[data-password-rule="${name}"]`);
            if (!item) continue;
            const valid = validate(value);
            item.classList.toggle('is-valid', valid);
            const icon = item.querySelector('i');
            if (icon) icon.className = valid ? 'bi bi-check-circle-fill' : 'bi bi-circle';
        }

        const confirmationMatches = value.length > 0 && value === confirmation.value;
        confirmation.setCustomValidity(confirmation.value && !confirmationMatches
            ? 'A confirmação da senha não corresponde à nova senha.'
            : '');
    }

    password.addEventListener('input', refreshRules);
    confirmation.addEventListener('input', refreshRules);
    refreshRules();
})();
