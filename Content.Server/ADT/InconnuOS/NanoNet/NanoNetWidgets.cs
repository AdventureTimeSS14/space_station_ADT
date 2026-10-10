namespace Content.Server.ADT.InconnuOS.NanoNet;

public static class NanoNetWidgets
{
    private const string Marker = "data-nn-widget";

    private const string Runtime = """
        <script>
        document.querySelectorAll('[data-nn-widget="tabs"]').forEach(function (root) {
            var panels = Array.prototype.slice.call(root.querySelectorAll(':scope > [data-nn-tab]'));
            if (!panels.length) return;

            var nav = document.createElement('div');
            nav.className = 'nn-tabs-nav';

            panels.forEach(function (panel, i) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'nn-tabs-btn' + (i === 0 ? ' nn-tabs-active' : '');
                btn.textContent = panel.getAttribute('data-nn-tab');

                btn.addEventListener('click', function () {
                    panels.forEach(function (p) { p.style.display = 'none'; });
                    nav.querySelectorAll('.nn-tabs-btn').forEach(function (b) { b.classList.remove('nn-tabs-active'); });
                    panel.style.display = '';
                    btn.classList.add('nn-tabs-active');
                });

                if (i !== 0) panel.style.display = 'none';
                nav.appendChild(btn);
            });

            root.insertBefore(nav, root.firstChild);
        });

        document.querySelectorAll('[data-nn-widget="gallery"]').forEach(function (root) {
            var slides = Array.prototype.slice.call(root.children);
            if (slides.length < 2) return;

            var index = 0;

            var nav = document.createElement('div');
            nav.className = 'nn-gallery-nav';

            var prev = document.createElement('button');
            prev.type = 'button';
            prev.className = 'nn-gallery-btn nn-gallery-prev';
            prev.textContent = '<';

            var next = document.createElement('button');
            next.type = 'button';
            next.className = 'nn-gallery-btn nn-gallery-next';
            next.textContent = '>';

            var counter = document.createElement('span');
            counter.className = 'nn-gallery-counter';

            function show(i) {
                slides.forEach(function (s, j) { s.style.display = j === i ? '' : 'none'; });
                counter.textContent = (i + 1) + ' / ' + slides.length;
            }

            prev.addEventListener('click', function () {
                index = (index - 1 + slides.length) % slides.length;
                show(index);
            });

            next.addEventListener('click', function () {
                index = (index + 1) % slides.length;
                show(index);
            });

            nav.appendChild(prev);
            nav.appendChild(counter);
            nav.appendChild(next);

            root.appendChild(nav);
            show(0);
        });

        document.querySelectorAll('[data-nn-widget="accordion"]').forEach(function (root) {
            var sections = Array.prototype.slice.call(root.querySelectorAll(':scope > [data-nn-section]'));
            if (!sections.length) return;

            sections.forEach(function (section) {
                var header = document.createElement('button');
                header.type = 'button';
                header.className = 'nn-accordion-header';
                header.textContent = section.getAttribute('data-nn-section');

                header.addEventListener('click', function () {
                    var isOpen = header.classList.contains('nn-accordion-open');

                    sections.forEach(function (s) { s.style.display = 'none'; });
                    root.querySelectorAll('.nn-accordion-header').forEach(function (h) { h.classList.remove('nn-accordion-open'); });

                    if (!isOpen) {
                        section.style.display = '';
                        header.classList.add('nn-accordion-open');
                    }
                });

                section.style.display = 'none';
                root.insertBefore(header, section);
            });
        });

        document.querySelectorAll('[data-nn-widget="random"]').forEach(function (root) {
            var lines = Array.prototype.slice.call(root.querySelectorAll(':scope > [data-nn-line]'));
            if (!lines.length) return;

            var chosen = lines[Math.floor(Math.random() * lines.length)];
            lines.forEach(function (line) { line.style.display = line === chosen ? '' : 'none'; });
        });

        document.querySelectorAll('[data-nn-widget="countdown"]').forEach(function (el) {
            var target = Date.parse(el.getAttribute('data-nn-until') || '');
            if (isNaN(target)) return;

            function tick() {
                var diff = target - Date.now();

                if (diff <= 0) {
                    el.textContent = 'Событие уже началось';
                    clearInterval(timer);
                    return;
                }

                var totalSeconds = Math.floor(diff / 1000);
                var days = Math.floor(totalSeconds / 86400);
                var hours = Math.floor((totalSeconds % 86400) / 3600);
                var minutes = Math.floor((totalSeconds % 3600) / 60);
                var seconds = totalSeconds % 60;

                el.textContent = days + 'д ' + hours + 'ч ' + minutes + 'м ' + seconds + 'с';
            }

            var timer = setInterval(tick, 1000);
            tick();
        });

        document.querySelectorAll('[data-nn-widget="modal"]').forEach(function (root) {
            var label = root.getAttribute('data-nn-modal');
            if (!label) return;

            var dialog = document.createElement('dialog');
            dialog.className = 'nn-modal-dialog';

            while (root.firstChild) {
                dialog.appendChild(root.firstChild);
            }

            var close = document.createElement('button');
            close.type = 'button';
            close.className = 'nn-modal-close';
            close.textContent = 'X';
            close.addEventListener('click', function () { dialog.close(); });
            dialog.insertBefore(close, dialog.firstChild);

            var trigger = document.createElement('button');
            trigger.type = 'button';
            trigger.className = 'nn-modal-btn';
            trigger.textContent = label;
            trigger.addEventListener('click', function () { dialog.showModal(); });

            root.appendChild(trigger);
            root.appendChild(dialog);
        });
        </script>
        """;

    public static string Apply(string html)
    {
        if (!html.Contains(Marker, StringComparison.Ordinal))
            return html;

        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        return bodyClose < 0
            ? html + Runtime
            : html[..bodyClose] + Runtime + html[bodyClose..];
    }
}
