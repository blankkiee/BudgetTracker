// Toolbar buttons reveal one form panel at a time. Panels use the hidden
// attribute so they stay inert to assistive tech while closed.
document.addEventListener('click', function (event) {
    if (event.target.closest('[data-sidebar-toggle]')) {
        document.body.classList.toggle('sidebar-open');
        return;
    }

    var trigger = event.target.closest('[data-panel-target]');
    if (!trigger) {
        return;
    }

    var target = document.getElementById(trigger.getAttribute('data-panel-target'));
    if (!target) {
        return;
    }

    var wasOpen = !target.hidden;

    document.querySelectorAll('[data-panel]').forEach(function (panel) {
        panel.hidden = true;
    });

    document.querySelectorAll('[data-panel-target]').forEach(function (button) {
        button.setAttribute('aria-expanded', 'false');
    });

    target.hidden = wasOpen;
    trigger.setAttribute('aria-expanded', String(!wasOpen));

    document.body.classList.remove('sidebar-open');

    if (!wasOpen) {
        target.scrollIntoView({ block: 'nearest' });
        var firstField = target.querySelector('input:not([type=hidden]), select');
        if (firstField) {
            firstField.focus();
        }
    }
});
