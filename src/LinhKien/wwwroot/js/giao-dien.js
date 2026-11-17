// Hiệu ứng giao diện. Mọi hiệu ứng đều phản hồi thao tác của người dùng.
(function () {
    'use strict';

    var giamChuyenDong = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ----- 1. Số trên biểu tượng giỏ nảy lên mỗi khi đổi -----
    var badge = document.getElementById('gioHangBadge');
    if (badge && window.MutationObserver && !giamChuyenDong) {
        new MutationObserver(function () {
            badge.classList.remove('bump');
            void badge.offsetWidth;                     // khởi động lại hoạt ảnh
            badge.classList.add('bump');
        }).observe(badge, { childList: true, characterData: true, subtree: true });
    }

    // ----- 2. Ảnh sản phẩm bay vào biểu tượng giỏ khi bấm "Thêm vào giỏ" -----
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-them-gio]');
        if (!btn || giamChuyenDong) return;

        var cfg = document.getElementById('cuaHangCfg');
        if (!cfg || cfg.dataset.auth !== '1') return;   // chưa đăng nhập: sẽ chuyển sang trang đăng nhập, không bay

        var gio = document.querySelector('a[href$="/gio-hang"]');
        var khung = btn.closest('.product-card, .detail-wrap');
        var anh = khung ? khung.querySelector('img') : null;
        if (!gio || !Element.prototype.animate) return;

        var tu = (anh || btn).getBoundingClientRect();
        var den = gio.getBoundingClientRect();
        var size = 72;
        var left = tu.left + tu.width / 2 - size / 2;
        var top = tu.top + tu.height / 2 - size / 2;
        var dx = den.left + den.width / 2 - (left + size / 2);
        var dy = den.top + den.height / 2 - (top + size / 2);

        var ghost = anh ? anh.cloneNode(false) : document.createElement('div');
        ghost.className = 'fly-ghost';
        ghost.removeAttribute('id');
        ghost.style.left = left + 'px';
        ghost.style.top = top + 'px';
        document.body.appendChild(ghost);

        var run = ghost.animate([
            { transform: 'translate(0,0) scale(1)', opacity: 1 },
            { transform: 'translate(' + dx + 'px,' + dy + 'px) scale(.2)', opacity: .35 }
        ], { duration: 650, easing: 'cubic-bezier(.45,0,.75,.35)' });
        run.onfinish = function () { ghost.remove(); };
    });

    // ----- 3. Phóng to ảnh theo vị trí con trỏ (soi chi tiết chân linh kiện) -----
    Array.prototype.forEach.call(document.querySelectorAll('[data-zoom]'), function (box) {
        var img = box.querySelector('img');
        if (!img) return;
        box.addEventListener('mousemove', function (e) {
            var r = box.getBoundingClientRect();
            img.style.transformOrigin = ((e.clientX - r.left) / r.width * 100) + '% ' + ((e.clientY - r.top) / r.height * 100) + '%';
        });
    });
})();
