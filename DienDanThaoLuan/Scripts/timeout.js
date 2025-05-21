document.addEventListener('DOMContentLoaded', function () {
    let lastInteractionTime = Date.now();

    // Cập nhật khi có hoạt động
    ['keypress', 'click'].forEach(event => {
        document.addEventListener(event, () => {
            const now = Date.now();
            const idleMinutes = (now - lastInteractionTime) / (60 * 1000);

            if (idleMinutes < 10) {
                // Gửi yêu cầu giữ phiên nếu còn trong giới hạn 10 phút
                fetch('/Account/KeepAlive', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }).then(response => {
                    if (response.ok) {
                        console.log('KeepAlive sent due to activity');
                    } else {
                        console.log('KeepAlive failed');
                    }
                }).catch(error => {
                    console.log('KeepAlive error:', error);
                });
            } else {
                console.log('No KeepAlive sent – too much idle time');
            }

            // Luôn cập nhật thời gian tương tác sau khi xử lý
            lastInteractionTime = now;
        });
    });
});
