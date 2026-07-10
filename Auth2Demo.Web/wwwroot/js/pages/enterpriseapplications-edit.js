(() => {
    const form = document.getElementById('enterpriseApplicationForm');
    const owner = document.getElementById('OwnerCompanyId');
    const applicationEnabled = document.getElementById('IsEnabled');
    const tenantCards = Array.from(document.querySelectorAll('[data-tenant-card]'));
    const providerCards = Array.from(document.querySelectorAll('[data-provider-card]'));
    const providerGroups = Array.from(document.querySelectorAll('[data-provider-group]'));

    const selectedTenantIds = () => new Set(tenantCards
        .filter(card => card.querySelector('.tenant-enabled-input')?.checked)
        .map(card => card.dataset.companyId));

    function refresh() {
        const isApplicationEnabled = applicationEnabled?.checked === true;
        const statusCard = document.getElementById('applicationStatusCard');
        const statusPill = document.getElementById('applicationStatusPill');
        const headerStatus = document.getElementById('headerApplicationStatus');
        const summaryStatus = document.getElementById('summaryApplicationStatus');
        const statusTitle = document.getElementById('applicationStatusTitle');
        const statusDescription = document.getElementById('applicationStatusDescription');
        const statusWarning = document.getElementById('applicationStatusWarning');

        [statusCard, statusPill, headerStatus].forEach(element => {
            element?.classList.toggle('is-enabled', isApplicationEnabled);
            element?.classList.toggle('is-disabled', !isApplicationEnabled);
        });
        if (statusPill) statusPill.innerHTML = isApplicationEnabled
            ? '<i class="bi bi-check-circle-fill"></i><span>Enabled</span>'
            : '<i class="bi bi-pause-circle-fill"></i><span>Disabled</span>';
        if (headerStatus) headerStatus.innerHTML = isApplicationEnabled
            ? '<i class="bi bi-check-circle"></i><strong>Enabled</strong>'
            : '<i class="bi bi-pause-circle"></i><strong>Disabled</strong>';
        if (summaryStatus) summaryStatus.textContent = isApplicationEnabled ? 'Enabled' : 'Disabled';
        if (statusTitle) statusTitle.textContent = isApplicationEnabled ? 'Application is available' : 'Application is disabled';
        if (statusDescription) statusDescription.textContent = isApplicationEnabled
            ? 'Users can access this application when tenant, provider and assignment policies allow it.'
            : 'New authorization requests are denied until an administrator enables this application.';
        if (statusWarning) statusWarning.hidden = isApplicationEnabled;

        const allowedTenants = selectedTenantIds();
        const ownerId = owner?.value || '';

        tenantCards.forEach(card => {
            const enabled = card.querySelector('.tenant-enabled-input')?.checked === true;
            card.classList.toggle('is-selected', enabled);
            card.classList.toggle('is-owner', !!ownerId && card.dataset.companyId === ownerId);
            card.querySelectorAll('.assignment-input, textarea').forEach(control => control.disabled = !enabled);
        });

        providerCards.forEach(card => {
            const input = card.querySelector('.provider-input');
            if (!input) return;
            const companyId = card.dataset.companyId || '';
            const tenantAllowed = !companyId || allowedTenants.has(companyId) || companyId === ownerId;
            const configured = !card.classList.contains('is-not-configured');
            input.disabled = !tenantAllowed || !configured;
            if (!tenantAllowed) input.checked = false;
            card.classList.toggle('is-tenant-disabled', !tenantAllowed);
            card.classList.toggle('is-selected', input.checked);
        });

        const tenantCount = tenantCards.filter(card => card.querySelector('.tenant-enabled-input')?.checked).length;
        const providerCount = providerCards.filter(card => card.querySelector('.provider-input')?.checked).length;
        const assignmentCount = tenantCards.filter(card => card.querySelector('.tenant-enabled-input')?.checked && card.querySelector('.assignment-input')?.checked).length;
        const ownerText = owner?.selectedOptions[0]?.text || 'Not assigned';

        document.getElementById('summaryOwner').textContent = owner?.value ? ownerText : 'Not assigned';
        document.getElementById('summaryTenants').textContent = tenantCount;
        document.getElementById('summaryProviders').textContent = providerCount;
        document.getElementById('summaryAssignments').textContent = assignmentCount;
        document.getElementById('headerTenantCount').textContent = tenantCount;
        document.getElementById('headerProviderCount').textContent = providerCount;

        const guidance = document.getElementById('summaryGuidance');
        if (guidance) {
            if (!owner?.value) guidance.innerHTML = '<i class="bi bi-exclamation-triangle"></i><span>Select an owner company to complete governance.</span>';
            else if (tenantCount === 0) guidance.innerHTML = '<i class="bi bi-exclamation-triangle"></i><span>Enable at least one tenant to allow organizational access to this application.</span>';
            else if (providerCount === 0) guidance.innerHTML = '<i class="bi bi-info-circle"></i><span>No external provider is selected. Local sign-in can still remain available.</span>';
            else guidance.innerHTML = '<i class="bi bi-check-circle"></i><span>The enterprise access policy is ready to save.</span>';
        }
    }

    tenantCards.forEach(card => {
        card.querySelector('.tenant-enabled-input')?.addEventListener('change', refresh);
        card.querySelector('.assignment-input')?.addEventListener('change', refresh);
    });
    providerCards.forEach(card => card.querySelector('.provider-input')?.addEventListener('change', refresh));
    owner?.addEventListener('change', refresh);
    applicationEnabled?.addEventListener('change', refresh);

    document.querySelector('[data-select-all-tenants]')?.addEventListener('click', () => {
        tenantCards.filter(card => !card.hidden).forEach(card => card.querySelector('.tenant-enabled-input').checked = true);
        refresh();
    });
    document.querySelector('[data-clear-tenants]')?.addEventListener('click', () => {
        tenantCards.forEach(card => card.querySelector('.tenant-enabled-input').checked = false);
        refresh();
    });
    document.querySelector('[data-select-valid-providers]')?.addEventListener('click', () => {
        providerCards.filter(card => !card.hidden).forEach(card => {
            const input = card.querySelector('.provider-input');
            if (input && !input.disabled) input.checked = true;
        });
        refresh();
    });
    document.querySelector('[data-clear-providers]')?.addEventListener('click', () => {
        providerCards.forEach(card => {
            const input = card.querySelector('.provider-input');
            if (input) input.checked = false;
        });
        refresh();
    });

    providerGroups.forEach(group => {
        group.querySelector('[data-toggle-provider-group]')?.addEventListener('click', () => {
            const inputs = Array.from(group.querySelectorAll('.provider-input:not(:disabled)'));
            const shouldSelect = inputs.some(input => !input.checked);
            inputs.forEach(input => input.checked = shouldSelect);
            refresh();
        });
    });

    function wireSearch(inputId, items, noResultsId, groupMode) {
        const input = document.getElementById(inputId);
        const noResults = document.getElementById(noResultsId);
        if (!input) return;
        input.addEventListener('input', () => {
            const term = input.value.trim().toLocaleLowerCase();
            let visible = 0;
            items.forEach(item => {
                const matches = !term || (item.dataset.search || '').includes(term);
                item.hidden = !matches;
                if (matches) visible++;
            });
            if (groupMode) {
                providerGroups.forEach(group => group.hidden = !Array.from(group.querySelectorAll('[data-provider-card]')).some(card => !card.hidden));
            }
            if (noResults) noResults.hidden = visible !== 0;
        });
    }

    wireSearch('tenantSearch', tenantCards, 'tenantNoResults', false);
    wireSearch('providerSearch', providerCards, 'providerNoResults', true);

    form?.addEventListener('submit', () => {
        tenantCards.forEach(card => {
            const enabled = card.querySelector('.tenant-enabled-input')?.checked === true;
            card.querySelectorAll('.assignment-input, textarea').forEach(control => control.disabled = !enabled);
        });
    });

    refresh();
})();
