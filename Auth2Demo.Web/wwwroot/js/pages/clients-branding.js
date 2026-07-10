(() => {
            const form = document.getElementById('clientBrandingForm');
            const stage = document.getElementById('brandingPreviewStage');
            const win = document.getElementById('brandingPreviewWindow');
            const logoBadge = document.getElementById('previewLogoBadge');
            const tenantName = document.getElementById('previewTenantName');
            const forgotLink = document.getElementById('previewForgotLink');
            const createAccount = document.getElementById('previewCreateAccount');
            const localLogin = document.getElementById('previewLocalLogin');
            const providerArea = document.getElementById('previewProviderArea');
            const providerList = document.getElementById('previewProviderList');
            const noMethods = document.getElementById('previewNoMethods');
            const methodCount = document.querySelector('[data-method-count]');

            const presets = {
                auth2demo: {
                    TenantName: 'Auth2Demo', PrimaryColor: '#2563EB', SecondaryColor: '#0F172A', BackgroundColor: '#EFF6FF', SurfaceColor: '#FFFFFF', TextColor: '#0F172A', MutedTextColor: '#64748B', BorderColor: '#CBD5E1', SuccessColor: '#16A34A', WarningColor: '#F59E0B', DangerColor: '#DC2626', Theme: 'Light', CardRadius: 28, ButtonRadius: 14, UseGradientButtons: true
                },
                corporate: {
                    TenantName: 'Corporate ID', PrimaryColor: '#1D4ED8', SecondaryColor: '#1E40AF', BackgroundColor: '#F8FAFC', SurfaceColor: '#FFFFFF', TextColor: '#0B1220', MutedTextColor: '#5B677A', BorderColor: '#D9E2EF', SuccessColor: '#15803D', WarningColor: '#B45309', DangerColor: '#B91C1C', Theme: 'Light', CardRadius: 22, ButtonRadius: 10, UseGradientButtons: false
                },
                slate: {
                    TenantName: 'Slate Portal', PrimaryColor: '#334155', SecondaryColor: '#0F172A', BackgroundColor: '#F1F5F9', SurfaceColor: '#FFFFFF', TextColor: '#0F172A', MutedTextColor: '#64748B', BorderColor: '#CBD5E1', SuccessColor: '#16A34A', WarningColor: '#D97706', DangerColor: '#DC2626', Theme: 'Light', CardRadius: 24, ButtonRadius: 12, UseGradientButtons: false
                },
                emerald: {
                    TenantName: 'Emerald Trust', PrimaryColor: '#047857', SecondaryColor: '#064E3B', BackgroundColor: '#ECFDF5', SurfaceColor: '#FFFFFF', TextColor: '#0F172A', MutedTextColor: '#64748B', BorderColor: '#A7F3D0', SuccessColor: '#059669', WarningColor: '#D97706', DangerColor: '#DC2626', Theme: 'Light', CardRadius: 26, ButtonRadius: 14, UseGradientButtons: true
                },
                indigo: {
                    TenantName: 'Indigo Pro', PrimaryColor: '#4F46E5', SecondaryColor: '#1E1B4B', BackgroundColor: '#EEF2FF', SurfaceColor: '#FFFFFF', TextColor: '#111827', MutedTextColor: '#64748B', BorderColor: '#C7D2FE', SuccessColor: '#16A34A', WarningColor: '#F59E0B', DangerColor: '#DC2626', Theme: 'Light', CardRadius: 30, ButtonRadius: 16, UseGradientButtons: true
                },
                dark: {
                    TenantName: 'Dark Enterprise', PrimaryColor: '#60A5FA', SecondaryColor: '#2563EB', BackgroundColor: '#020617', SurfaceColor: '#0F172A', TextColor: '#E5E7EB', MutedTextColor: '#94A3B8', BorderColor: '#1F2937', SuccessColor: '#22C55E', WarningColor: '#FBBF24', DangerColor: '#F87171', Theme: 'Dark', CardRadius: 30, ButtonRadius: 14, UseGradientButtons: true
                }
            };

            function setValue(name, value) {
                const input = form.querySelector(`[name="${name}"]`);
                if (!input) return;
                if (input.type === 'checkbox') input.checked = !!value;
                else input.value = value;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }

            function getValue(name) {
                const input = form.querySelector(`[name="${name}"]`);
                if (!input) return '';
                return input.type === 'checkbox' ? input.checked : input.value;
            }

            function selectedProviders() {
                return Array.from(form.querySelectorAll('[data-auth-provider]:checked'))
                    .map(input => ({ scheme: input.dataset.providerScheme || input.value, name: input.dataset.providerName || input.value }));
            }

            function renderPreviewProviders(providers) {
                providerList.innerHTML = providers.map(provider => {
                    const initial = (provider.name || provider.scheme || '?').substring(0, 1).toUpperCase();
                    const label = (provider.name || provider.scheme || '').replace(/</g, '&lt;').replace(/>/g, '&gt;');
                    return `<button class="btn btn-outline btn-block external-btn" type="button"><span class="provider-dot">${initial}</span> ${label}</button>`;
                }).join('');
            }

            function updatePreview() {
                const primary = getValue('PrimaryColor') || '#2563EB';
                const secondary = getValue('SecondaryColor') || '#0F172A';
                const bg = getValue('BackgroundColor') || '#EFF6FF';
                const surface = getValue('SurfaceColor') || '#FFFFFF';
                const text = getValue('TextColor') || '#111827';
                const muted = getValue('MutedTextColor') || '#64748B';
                const border = getValue('BorderColor') || '#CBD5E1';
                const cardRadius = getValue('CardRadius') || '28';
                const buttonRadius = getValue('ButtonRadius') || '14';
                const useGradient = getValue('UseGradientButtons');
                const theme = getValue('Theme');
                const logo = getValue('LogoUrl');
                const name = getValue('TenantName') || form.dataset.clientTitle || 'Auth2Demo';

                stage.style.setProperty('--primary', primary);
                stage.style.setProperty('--primary-2', secondary);
                stage.style.setProperty('--bg', bg);
                stage.style.setProperty('--surface', surface);
                stage.style.setProperty('--surface-2', surface);
                stage.style.setProperty('--text', text);
                stage.style.setProperty('--muted', muted);
                stage.style.setProperty('--line', border);
                stage.style.setProperty('--auth-card-radius', `${cardRadius}px`);
                stage.style.setProperty('--auth-button-radius', `${buttonRadius}px`);
                stage.style.setProperty('--auth-button-background', useGradient ? `linear-gradient(135deg, ${primary}, ${secondary})` : primary);
                stage.dataset.theme = theme;
                tenantName.textContent = name;
                const localEnabled = getValue('EnableLocalLogin');
                const providers = selectedProviders();
                renderPreviewProviders(providers);
                localLogin.style.display = localEnabled ? '' : 'none';
                providerArea.style.display = providers.length ? '' : 'none';
                noMethods.style.display = (!localEnabled && providers.length === 0) ? '' : 'none';
                if (methodCount) methodCount.textContent = (providers.length + (localEnabled ? 1 : 0)).toString();
                forgotLink.style.display = (localEnabled && getValue('ShowForgotPasswordLink')) ? '' : 'none';
                createAccount.style.display = (localEnabled && getValue('ShowCreateAccountLink')) ? '' : 'none';
                logoBadge.innerHTML = logo ? `<img src="${logo.replace(/"/g, '&quot;')}" alt="${name.replace(/"/g, '&quot;')}" />` : '<i class="bi bi-shield-lock" aria-hidden="true"></i>';
            }

            form.querySelectorAll('[data-color-field]').forEach(field => {
                const picker = field.querySelector('[data-color-picker]');
                const text = field.querySelector('[data-color-text]');
                const sync = source => {
                    const value = source.value.trim();
                    if (/^#[0-9a-fA-F]{6}$/.test(value)) {
                        picker.value = value;
                        text.value = value.toUpperCase();
                        updatePreview();
                    }
                };
                picker.addEventListener('input', () => sync(picker));
                text.addEventListener('input', () => sync(text));
            });

            form.querySelectorAll('[data-preview-token]').forEach(input => {
                input.addEventListener('input', updatePreview);
                input.addEventListener('change', updatePreview);
            });

            form.querySelectorAll('[data-range-value]').forEach(label => {
                const name = label.dataset.rangeValue;
                const input = form.querySelector(`[name="${name}"]`);
                if (input) input.addEventListener('input', () => label.textContent = input.value);
            });

            document.querySelectorAll('[data-tab-target]').forEach(tab => {
                tab.addEventListener('click', () => {
                    document.querySelectorAll('[data-tab-target]').forEach(x => x.classList.remove('active'));
                    document.querySelectorAll('[data-tab-panel]').forEach(x => x.classList.remove('active'));
                    tab.classList.add('active');
                    document.querySelector(`[data-tab-panel="${tab.dataset.tabTarget}"]`)?.classList.add('active');
                });
            });

            document.querySelectorAll('[data-preset]').forEach(button => {
                button.addEventListener('click', () => {
                    const preset = presets[button.dataset.preset];
                    Object.entries(preset).forEach(([name, value]) => setValue(name, value));
                });
            });

            document.querySelectorAll('[data-copy-color]').forEach(button => {
                button.addEventListener('click', async () => {
                    const value = button.closest('[data-color-field]')?.querySelector('[data-color-text]')?.value;
                    if (value && navigator.clipboard) await navigator.clipboard.writeText(value);
                });
            });

            document.querySelectorAll('[data-device]').forEach(button => {
                button.addEventListener('click', () => {
                    document.querySelectorAll('[data-device]').forEach(x => x.classList.remove('active'));
                    button.classList.add('active');
                    win.classList.toggle('is-mobile', button.dataset.device === 'mobile');
                });
            });

            updatePreview();
        })();
