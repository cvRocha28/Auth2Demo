(() => {
        const form = document.querySelector('.client-form-builder');
        if (!form) return;
        const config = form.dataset;
const templates = {
            secret: (prefix, index) => `<div class="dynamic-row grid-secret">
                <input name="${prefix}[${index}].Description" placeholder="${config.secretDescriptionPlaceholder}" />
                <select name="${prefix}[${index}].Expiration">
                    <option value="Never">${config.neverExpires}</option>
                    <option value="Months6">${config.sixMonths}</option>
                    <option value="Months12">${config.twelveMonths}</option>
                    <option value="Months24">${config.twentyFourMonths}</option>
                </select>
                <input name="${prefix}[${index}].PlainTextSecret" placeholder="${config.secretValueAutoPlaceholder}" />
                <input type="hidden" name="${prefix}[${index}].IsExisting" value="false" />
                <button class="btn icon danger js-remove-row" type="button" title="${config.removeSecret}"><i class="bi bi-trash"></i></button>
            </div>`,

            claim: (prefix, index) => `<div class="dynamic-row grid-claim">
                <input name="${prefix}[${index}].Type" placeholder="${config.claimTypePlaceholder}" />
                <input name="${prefix}[${index}].Value" placeholder="${config.claimValuePlaceholder}" />
                <button class="btn icon danger js-remove-row" type="button" title="${config.removeClaim}"><i class="bi bi-trash"></i></button>
            </div>`,

            grant: (prefix, index) => `<div class="dynamic-row input-add-row">
                <select name="${prefix}[${index}]">
                    <option value="authorization_code">authorization_code</option>
                    <option value="refresh_token">refresh_token</option>
                    <option value="client_credentials">client_credentials</option>
                </select>
                <button class="btn icon danger js-remove-row" type="button" title="${config.removeGrantType}"><i class="bi bi-trash"></i></button>
            </div>`,

            string: (prefix, index, placeholder) => `<div class="dynamic-row input-add-row">
                <input name="${prefix}[${index}]" placeholder="${placeholder}" />
                <button class="btn icon danger js-remove-row" type="button" title="${config.removeItem}"><i class="bi bi-trash"></i></button>
            </div>`
        };

        function reindex(list) {
            const prefix = list.dataset.list;

            list.querySelectorAll('.dynamic-items > .dynamic-row').forEach((row, index) => {
                row.querySelectorAll('[name]').forEach(input => {
                    input.name = input.name.replace(new RegExp(prefix + '\\[\\d+\\]'), `${prefix}[${index}]`);
                });
            });
        }

        const presets = {
            [config.webKind]: {
                clientType: config.confidentialType,
                consentType: config.explicitConsent,
                grants: ['authorization_code', 'refresh_token'],
                scopes: ['openid', 'profile', 'email', 'roles', 'offline_access'],
                redirects: ['https://localhost:7108/signin-oidc'],
                postLogoutRedirects: ['https://localhost:7108/signout-callback-oidc'],
                endpoints: { authorization: true, token: true, endSession: true, revocation: true, introspection: true },
                requirePkce: true,
                showSecrets: true,
                showRedirects: true,
                secretDescription: 'default',
                secretExpiration: 'Months12'
            },
            [config.spaKind]: {
                clientType: config.publicType,
                consentType: config.explicitConsent,
                grants: ['authorization_code', 'refresh_token'],
                scopes: ['openid', 'profile', 'email', 'offline_access'],
                redirects: ['https://localhost:5173/auth/callback'],
                postLogoutRedirects: ['https://localhost:5173/'],
                endpoints: { authorization: true, token: true, endSession: true, revocation: true, introspection: false },
                requirePkce: true,
                showSecrets: false,
                showRedirects: true
            },
            [config.m2mKind]: {
                clientType: config.confidentialType,
                consentType: config.implicitConsent,
                grants: ['client_credentials'],
                scopes: ['api'],
                redirects: [],
                postLogoutRedirects: [],
                endpoints: { authorization: false, token: true, endSession: false, revocation: true, introspection: true },
                requirePkce: false,
                showSecrets: true,
                showRedirects: false,
                secretDescription: 'm2m-default',
                secretExpiration: 'Months12'
            },
            [config.nativeKind]: {
                clientType: config.publicType,
                consentType: config.explicitConsent,
                grants: ['authorization_code', 'refresh_token'],
                scopes: ['openid', 'profile', 'email', 'offline_access'],
                redirects: ['http://127.0.0.1:7890/callback'],
                postLogoutRedirects: ['http://127.0.0.1:7890/'],
                endpoints: { authorization: true, token: true, endSession: true, revocation: true, introspection: false },
                requirePkce: true,
                showSecrets: false,
                showRedirects: true
            }
        };

        function getList(name) {
            return form.querySelector(`[data-list="${name}"]`);
        }

        function setDynamicValues(listName, values, templateType, placeholder = '') {
            const list = getList(listName);
            const container = list?.querySelector('.dynamic-items');
            if (!container) return;

            container.innerHTML = '';
            values.forEach((value, index) => {
                container.insertAdjacentHTML('beforeend', templates[templateType](listName, index, placeholder));
                const row = container.lastElementChild;
                const input = row?.querySelector('input, select');
                if (input) input.value = value;
            });
        }

        function setEndpoint(name, value) {
            const input = form.querySelector(`[name="${name}"]`);
            if (input) input.checked = value;
        }

        function ensureSecretRow(preset) {
            const list = getList('SecretItems');
            const container = list?.querySelector('.dynamic-items');
            if (!container || container.querySelector('.dynamic-row')) return;

            container.insertAdjacentHTML('beforeend', templates.secret('SecretItems', 0));
            const row = container.lastElementChild;
            const description = row?.querySelector('[name="SecretItems[0].Description"]');
            const expiration = row?.querySelector('[name="SecretItems[0].Expiration"]');
            if (description) description.value = preset.secretDescription || 'default';
            if (expiration) expiration.value = preset.secretExpiration || 'Months12';
        }

        function applyApplicationPreset() {
            const kind = form.querySelector('.js-client-kind')?.value || config.webKind;
            const preset = presets[kind] || presets[config.webKind];
            const clientType = form.querySelector('.js-client-type');
            const consentType = form.querySelector('[name="ConsentType"]');
            const secretPanel = form.querySelector('.js-secret-panel');
            const redirectPanel = form.querySelector('.js-redirect-panel');
            const pkce = form.querySelector('[name="RequirePkce"]');

            if (clientType) clientType.value = preset.clientType;
            if (consentType) consentType.value = preset.consentType;
            if (pkce) pkce.checked = preset.requirePkce;

            setDynamicValues('GrantTypeItems', preset.grants, 'grant');
            setDynamicValues('ScopeItems', preset.scopes, 'string', 'openid');
            setDynamicValues('RedirectUriItems', preset.redirects, 'string', 'https://localhost:7108/signin-oidc');
            setDynamicValues('PostLogoutRedirectUriItems', preset.postLogoutRedirects, 'string', 'https://localhost:7108/signout-callback-oidc');

            setEndpoint('AllowAuthorizationEndpoint', preset.endpoints.authorization);
            setEndpoint('AllowTokenEndpoint', preset.endpoints.token);
            setEndpoint('AllowEndSessionEndpoint', preset.endpoints.endSession);
            setEndpoint('AllowRevocationEndpoint', preset.endpoints.revocation);
            setEndpoint('AllowIntrospectionEndpoint', preset.endpoints.introspection);

            if (secretPanel) secretPanel.style.display = preset.showSecrets ? '' : 'none';
            if (redirectPanel) redirectPanel.style.display = preset.showRedirects ? '' : 'none';
            if (preset.showSecrets) ensureSecretRow(preset);
        }

        function syncClientMode() {
            const clientType = form.querySelector('.js-client-type');
            const secretPanel = form.querySelector('.js-secret-panel');
            const kind = form.querySelector('.js-client-kind')?.value || '';
            const isMachineToMachine = kind === config.m2mKind;
            const isPublic = clientType?.value === config.publicType;

            if (secretPanel) secretPanel.style.display = isPublic ? 'none' : '';
            const redirectPanel = form.querySelector('.js-redirect-panel');
            if (redirectPanel) redirectPanel.style.display = isMachineToMachine ? 'none' : '';
        }

        form.addEventListener('click', event => {
            const addButton = event.target.closest('.js-add-row');
            const removeButton = event.target.closest('.js-remove-row');

            if (addButton) {
                const list = addButton.closest('.dynamic-list');
                const prefix = list.dataset.list;
                const index = list.querySelectorAll('.dynamic-items > .dynamic-row').length;
                const type = addButton.dataset.template;
                const placeholder = addButton.dataset.placeholder || '';

                list.querySelector('.dynamic-items').insertAdjacentHTML(
                    'beforeend',
                    templates[type](prefix, index, placeholder)
                );

                event.preventDefault();
            }

            if (removeButton) {
                const list = removeButton.closest('.dynamic-list');
                removeButton.closest('.dynamic-row').remove();
                reindex(list);
                event.preventDefault();
            }
        });

        form.querySelector('.js-client-kind')?.addEventListener('change', applyApplicationPreset);
        form.querySelector('.js-client-type')?.addEventListener('change', syncClientMode);

        syncClientMode();
    })();
