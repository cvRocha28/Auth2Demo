const page = document.querySelector('.client-details-page');
        document.querySelectorAll('[data-copy]').forEach(button => {
            button.addEventListener('click', async () => {
                const value = button.getAttribute('data-copy');
                if (!value) return;

                try {
                    await navigator.clipboard.writeText(value);
                    const previous = button.innerHTML;
                    button.innerHTML = `<i class="bi bi-check-lg" aria-hidden="true"></i> ${page?.dataset.copiedLabel || 'Copied'}`;
                    setTimeout(() => button.innerHTML = previous, 1200);
                } catch {
                    window.prompt(page?.dataset.copyValuePrompt || 'Copy value', value);
                }
            });
        });
