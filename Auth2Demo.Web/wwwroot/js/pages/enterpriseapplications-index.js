(() => {
    const input = document.getElementById('enterpriseApplicationSearch');
    const rows = Array.from(document.querySelectorAll('[data-enterprise-row]'));
    const empty = document.getElementById('enterpriseNoResults');
    if (!input || rows.length === 0) return;

    input.addEventListener('input', () => {
        const term = input.value.trim().toLocaleLowerCase();
        let visible = 0;
        rows.forEach(row => {
            const matches = !term || (row.dataset.search || '').includes(term);
            row.hidden = !matches;
            if (matches) visible++;
        });
        if (empty) empty.hidden = visible !== 0;
    });
})();
