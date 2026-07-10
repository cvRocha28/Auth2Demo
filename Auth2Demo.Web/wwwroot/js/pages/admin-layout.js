(() => {
            const shell = document.querySelector('.admin-app-shell');
            const button = document.querySelector('[data-admin-menu-button]');
            button?.addEventListener('click', () => shell?.classList.toggle('admin-menu-open'));

            const menu = document.querySelector('[data-admin-menu]');
            if (!menu) return;

            const groups = Array.from(menu.querySelectorAll('.sidebar-group'));
            groups.forEach(group => {
                group.addEventListener('toggle', () => {
                    if (!group.open) return;
                    groups.forEach(otherGroup => {
                        if (otherGroup !== group) otherGroup.open = false;
                    });
                });
            });
        })();
