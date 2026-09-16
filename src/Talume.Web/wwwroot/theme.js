// Run before CSS so the saved theme is applied before the first paint.
(() => {
    const key = 'talume.theme';
    const root = document.documentElement;
    const moon = 'M20.9 13A9 9 0 0 1 11 3.1 9 9 0 1 0 20.9 13Z';
    const sun = 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8 M12 2v2 M12 20v2 M2 12h2 M20 12h2 M4.9 4.9l1.4 1.4 M17.7 17.7l1.4 1.4 M4.9 19.1l1.4-1.4 M17.7 6.3l1.4-1.4';
    function sync() {
        const dark = root.dataset.theme === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(button => {
            button.setAttribute('aria-label', 'Tema escuro');
            button.setAttribute('aria-pressed', String(dark));
            button.title = dark ? 'Voltar ao tema claro' : 'Ativar tema escuro';
            button.innerHTML = `<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="${dark ? sun : moon}"/></svg><span>${dark ? 'Tema claro' : 'Tema escuro'}</span>`;
        });
    }
    function apply(theme) { root.dataset.theme = theme === 'dark' ? 'dark' : 'light'; sync(); }
    let saved = 'light';
    try { saved = localStorage.getItem(key) || 'light'; } catch { /* Storage may be disabled. */ }
    apply(saved);
    document.addEventListener('click', event => {
        if (!event.target.closest('[data-theme-toggle]')) return;
        const theme = root.dataset.theme === 'dark' ? 'light' : 'dark';
        apply(theme);
        try { localStorage.setItem(key, theme); } catch { /* Switching still works in this page. */ }
    });
    window.addEventListener('storage', event => { if (event.key === key) apply(event.newValue); });
    document.addEventListener('DOMContentLoaded', sync);
    window.TalumeTheme = { sync };
})();
