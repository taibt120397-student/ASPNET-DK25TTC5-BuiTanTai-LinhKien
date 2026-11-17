// Thêm vào giỏ bằng AJAX, số lượng trên biểu tượng giỏ, gợi ý tìm kiếm.
(function () {
    'use strict';

    var cfg = document.getElementById('cuaHangCfg');
    var dangNhap = !!cfg && cfg.dataset.auth === '1';
    var badge = document.getElementById('gioHangBadge');

    function token() {
        var i = document.querySelector('#afForm input[name="__RequestVerificationToken"]');
        return i ? i.value : '';
    }

    function capNhatBadge(n) {
        if (!badge) return;
        badge.textContent = n;
        badge.classList.toggle('d-none', !(n > 0));
    }

    // ----- Số dòng trong giỏ khi mở trang -----
    if (dangNhap) {
        fetch('/gio-hang/so-luong', { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (d) { capNhatBadge(d.count); })
            .catch(function () { });
    }

    // ----- Thông báo nhỏ góc phải dưới -----
    function toast(msg, ok) {
        var box = document.getElementById('toastBox');
        if (!box) {
            box = document.createElement('div');
            box.id = 'toastBox';
            box.className = 'position-fixed bottom-0 end-0 p-3';
            box.style.zIndex = 1100;
            document.body.appendChild(box);
        }
        var el = document.createElement('div');
        el.className = 'toast align-items-center border-0 text-bg-' + (ok ? 'success' : 'danger');
        el.setAttribute('role', 'alert');
        var wrap = document.createElement('div');
        wrap.className = 'd-flex';
        var body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = msg;               // textContent: không chèn HTML từ máy chủ
        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close btn-close-white me-2 m-auto';
        close.setAttribute('data-bs-dismiss', 'toast');
        wrap.appendChild(body);
        wrap.appendChild(close);
        el.appendChild(wrap);
        box.appendChild(el);

        if (window.bootstrap && window.bootstrap.Toast) {
            el.addEventListener('hidden.bs.toast', function () { el.remove(); });
            new window.bootstrap.Toast(el, { delay: 3500 }).show();
        } else {
            alert(msg);
            el.remove();
        }
    }

    // ----- Nút "Thêm vào giỏ" -----
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-them-gio]');
        if (!btn) return;
        e.preventDefault();

        var sl = 1;
        if (btn.dataset.qty) {
            var inp = document.querySelector(btn.dataset.qty);
            if (inp) sl = parseInt(inp.value, 10) || 1;
        }

        btn.disabled = true;
        fetch('/gio-hang/them', {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'RequestVerificationToken': token(),
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: new URLSearchParams({ sanPhamId: btn.dataset.id, soLuong: sl })
        })
            .then(function (r) {
                if (r.status === 401) {
                    location.href = '/Identity/Account/Login?ReturnUrl=' +
                        encodeURIComponent(location.pathname + location.search);
                    return null;
                }
                return r.json();
            })
            .then(function (d) {
                if (!d) return;
                toast(d.thongBao, d.ok);
                capNhatBadge(d.dem);
            })
            .catch(function () { toast('Không kết nối được máy chủ, vui lòng thử lại.', false); })
            .finally(function () { btn.disabled = false; });
    });

    // ----- Gợi ý tìm kiếm: áp dụng cho mọi ô có thuộc tính data-goi-y -----
    function khoiTaoGoiY(input) {
        var cha = input.parentElement;
        if (getComputedStyle(cha).position === 'static') cha.style.position = 'relative';

        var list = document.createElement('div');
        list.className = 'list-group position-absolute shadow d-none';
        list.style.cssText = 'top:100%;left:0;right:0;z-index:1050;max-height:420px;overflow-y:auto;min-width:300px';
        cha.appendChild(list);

        var timer = null;
        var yeuCauMoiNhat = 0;

        function an() { list.classList.add('d-none'); list.innerHTML = ''; }

        function hienThi(items) {
            list.innerHTML = '';
            if (!items.length) { an(); return; }   // không có gợi ý: để người dùng bấm Tìm kiếm
            items.forEach(function (it) {
                var a = document.createElement('a');
                a.className = 'list-group-item list-group-item-action d-flex align-items-center gap-2 py-2';
                a.href = it.url;

                if (it.hinhAnh) {
                    var img = document.createElement('img');
                    img.src = it.hinhAnh;
                    img.width = 36; img.height = 36;
                    img.style.objectFit = 'cover';
                    img.className = 'rounded';
                    a.appendChild(img);
                }
                var txt = document.createElement('div');
                txt.className = 'flex-grow-1';
                var ten = document.createElement('div');
                ten.className = 'fw-semibold';
                ten.textContent = it.ten;            // textContent: không chèn HTML từ máy chủ
                var phu = document.createElement('div');
                phu.className = 'small text-muted';
                phu.textContent = it.maSP + ' · ' + it.gia;
                txt.appendChild(ten);
                txt.appendChild(phu);
                a.appendChild(txt);
                list.appendChild(a);
            });
            list.classList.remove('d-none');
        }

        input.addEventListener('input', function () {
            clearTimeout(timer);
            var q = input.value.trim();
            if (q.length < 2) { an(); return; }
            timer = setTimeout(function () {
                var id = ++yeuCauMoiNhat;            // bỏ qua kết quả của yêu cầu cũ trả về chậm
                fetch('/san-pham/goi-y?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
                    .then(function (r) { return r.json(); })
                    .then(function (items) { if (id === yeuCauMoiNhat) hienThi(items); })
                    .catch(function () { });
            }, 250);
        });
        input.addEventListener('keydown', function (e) { if (e.key === 'Escape') an(); });
        document.addEventListener('click', function (e) { if (!cha.contains(e.target)) an(); });
    }

    Array.prototype.forEach.call(document.querySelectorAll('input[data-goi-y]'), khoiTaoGoiY);
})();
