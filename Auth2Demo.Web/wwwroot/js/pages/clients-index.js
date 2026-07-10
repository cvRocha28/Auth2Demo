(() => {
        const table = document.getElementById('clientsTable');
        if (!table) return;

        const rows = Array.from(table.querySelectorAll('tbody tr.client-row'));
        const search = document.getElementById('clientSearch');
        const type = document.getElementById('clientTypeFilter');
        const grant = document.getElementById('clientGrantFilter');
        const branding = document.getElementById('clientBrandingFilter');
        const count = document.getElementById('clientResultCount');
        const empty = document.getElementById('clientsEmptyState');
        const clear = document.getElementById('clearClientFilters');

        const normalize = value => (value || '').trim().toLowerCase();

        function applyFilters() {
            const q = normalize(search.value);
            const selectedType = normalize(type.value);
            const selectedGrant = normalize(grant.value);
            const selectedBranding = normalize(branding.value);
            let visible = 0;

            rows.forEach(row => {
                const matchesSearch = !q || row.dataset.search.includes(q);
                const matchesType = !selectedType || row.dataset.type === selectedType;
                const matchesGrant = !selectedGrant || row.dataset.grants.includes(selectedGrant);
                const matchesBranding = !selectedBranding || row.dataset.branding === selectedBranding;
                const show = matchesSearch && matchesType && matchesGrant && matchesBranding;
                row.hidden = !show;
                if (show) visible++;
            });

            count.textContent = visible === 1 ? '1 client' : `${visible} clients`;
            empty.hidden = visible !== 0;
        }

        [search, type, grant, branding].forEach(input => input.addEventListener('input', applyFilters));
        clear.addEventListener('click', () => {
            search.value = '';
            type.value = '';
            grant.value = '';
            branding.value = '';
            applyFilters();
            search.focus();
        });
    })();
