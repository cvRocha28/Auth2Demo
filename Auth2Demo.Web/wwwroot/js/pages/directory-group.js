(() => {
    const search = document.getElementById('memberSearch');
    const rows = Array.from(document.querySelectorAll('.group-member-row'));
    const empty = document.getElementById('memberSearchEmpty');
    const count = document.getElementById('memberSearchCount');
    const description = document.getElementById('groupDescription');
    const descriptionCount = document.getElementById('descriptionCount');

    if (search) {
        search.addEventListener('input', () => {
            const term = search.value.trim().toLowerCase();
            let visible = 0;
            rows.forEach(row => {
                const match = !term || (row.dataset.search || '').includes(term);
                row.classList.toggle('d-none', !match);
                if (match) visible++;
            });
            empty?.classList.toggle('d-none', visible !== 0);
            if (count) count.textContent = `${visible} member(s)`;
        });
    }

    if (description && descriptionCount) {
        description.addEventListener('input', () => {
            descriptionCount.textContent = description.value.length.toString();
        });
    }
})();
