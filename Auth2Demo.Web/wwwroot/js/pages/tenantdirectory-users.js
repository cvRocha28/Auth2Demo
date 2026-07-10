(() => {
    const search = document.getElementById('tenantUserSearch');
    const status = document.getElementById('tenantUserStatus');
    const rows = Array.from(document.querySelectorAll('[data-user-row]'));
    const noResults = document.getElementById('tenantUsersNoResults');
    if (!search || !status || rows.length === 0) return;

    const filter = () => {
        const term = search.value.trim().toLowerCase();
        const selected = status.value;
        let visible = 0;
        rows.forEach(row => {
            const matchesSearch = !term || row.dataset.search.includes(term);
            const rowStatus = row.dataset.status;
            const matchesStatus = selected === 'all' || rowStatus === selected || (selected === 'active' && rowStatus === 'default');
            const show = matchesSearch && matchesStatus;
            row.classList.toggle('d-none', !show);
            if (show) visible++;
        });
        noResults?.classList.toggle('d-none', visible !== 0);
    };
    search.addEventListener('input', filter);
    status.addEventListener('change', filter);
})();
