let tryonVideo = null;
let tryonCanvas = null;
let tryonCtx = null;
let tryonStream = null;
let tryonFaceMesh = null;
let selectedGlassesImage = null;
let isTryonRunning = false;

const TRYON_SCALE = 2.15;
const TRYON_Y_OFFSET_RATIO = 0.06;

document.addEventListener("DOMContentLoaded", function () {
    tryonVideo = document.getElementById("tryonVideo");
    tryonCanvas = document.getElementById("tryonCanvas");

    if (tryonCanvas) {
        tryonCtx = tryonCanvas.getContext("2d");
    }

    const btnCamera = document.getElementById("btnTryonCamera");

    if (btnCamera) {
        btnCamera.addEventListener("click", toggleTryonCamera);
    }

    const glassButtons = document.querySelectorAll(".tryon-glass-item");

    glassButtons.forEach(function (btn, index) {
        btn.addEventListener("click", function () {
            selectGlasses(btn);
        });

        if (index === 0) {
            selectGlasses(btn);
        }
    });
});

async function initTryonFaceMesh() {
    if (tryonFaceMesh) return;

    setTryonStatus("Đang tải AI...", "Hệ thống đang khởi tạo nhận diện khuôn mặt.");

    tryonFaceMesh = new FaceMesh({
        locateFile: function (file) {
            return `https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/${file}`;
        }
    });

    tryonFaceMesh.setOptions({
        maxNumFaces: 1,
        refineLandmarks: true,
        minDetectionConfidence: 0.5,
        minTrackingConfidence: 0.5
    });

    tryonFaceMesh.onResults(onTryonResults);
}

async function toggleTryonCamera() {
    if (tryonStream) {
        stopTryonCamera();
        return;
    }

    await startTryonCamera();
}

async function startTryonCamera() {
    try {
        await initTryonFaceMesh();

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            setTryonStatus("Không hỗ trợ camera", "Hãy dùng Chrome hoặc Edge phiên bản mới.");
            return;
        }

        tryonStream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "user",
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false
        });

        tryonVideo.srcObject = tryonStream;
        await tryonVideo.play();

        hideTryonPlaceholder();
        updateTryonCameraButton(true);
        setTryonStatus("Camera đã sẵn sàng", "Hãy nhìn thẳng và chọn mẫu kính bạn muốn thử.");

        if (!isTryonRunning) {
            isTryonRunning = true;
            runTryonLoop();
        }

    } catch (err) {
        console.error(err);

        let message = "Không mở được camera.";

        if (err.name === "NotAllowedError") {
            message = "Bạn đã chặn quyền camera. Hãy cho phép Camera trên trình duyệt.";
        } else if (err.name === "NotFoundError") {
            message = "Không tìm thấy camera trên thiết bị.";
        }

        setTryonStatus("Không mở được camera", message);
    }
}

function stopTryonCamera() {
    if (tryonStream) {
        tryonStream.getTracks().forEach(function (track) {
            track.stop();
        });
    }

    tryonStream = null;
    isTryonRunning = false;

    if (tryonVideo) {
        tryonVideo.pause();
        tryonVideo.srcObject = null;
    }

    if (tryonCtx && tryonCanvas) {
        tryonCtx.clearRect(0, 0, tryonCanvas.width, tryonCanvas.height);
    }

    updateTryonCameraButton(false);
    showTryonPlaceholder();

    setTryonStatus("Camera đã tắt", "Bạn có thể bật lại camera để tiếp tục thử kính.");
}

async function runTryonLoop() {
    if (!tryonStream || !tryonVideo || !tryonFaceMesh) {
        isTryonRunning = false;
        return;
    }

    try {
        await tryonFaceMesh.send({ image: tryonVideo });
    } catch (err) {
        console.error("Try-on loop error:", err);
    }

    requestAnimationFrame(runTryonLoop);
}

function onTryonResults(results) {
    if (!tryonCanvas || !tryonCtx || !tryonVideo) return;

    const width = tryonCanvas.clientWidth;
    const height = tryonCanvas.clientHeight;

    tryonCanvas.width = width;
    tryonCanvas.height = height;

    tryonCtx.clearRect(0, 0, width, height);

    drawMirroredVideo(width, height);

    if (!results.multiFaceLandmarks || results.multiFaceLandmarks.length === 0) {
        setTryonStatus("Chưa thấy khuôn mặt", "Hãy đưa khuôn mặt vào giữa khung camera.");
        return;
    }

    if (!selectedGlassesImage || !selectedGlassesImage.complete) {
        setTryonStatus("Chưa chọn mẫu kính", "Hãy chọn một mẫu kính ở danh sách bên phải.");
        return;
    }

    const lm = results.multiFaceLandmarks[0];

    drawGlassesOnMirroredFace(lm, width, height);

    setTryonStatus("Đang thử kính", "Bạn có thể đổi mẫu kính bất cứ lúc nào.");
}

function drawMirroredVideo(width, height) {
    tryonCtx.save();
    tryonCtx.translate(width, 0);
    tryonCtx.scale(-1, 1);
    tryonCtx.drawImage(tryonVideo, 0, 0, width, height);
    tryonCtx.restore();
}

function drawGlassesOnMirroredFace(lm, canvasWidth, canvasHeight) {
    const leftEyeOuter = lm[33];
    const rightEyeOuter = lm[263];

    if (!leftEyeOuter || !rightEyeOuter) return;

    const leftX = (1 - leftEyeOuter.x) * canvasWidth;
    const leftY = leftEyeOuter.y * canvasHeight;

    const rightX = (1 - rightEyeOuter.x) * canvasWidth;
    const rightY = rightEyeOuter.y * canvasHeight;

    const centerX = (leftX + rightX) / 2;
    const centerY = (leftY + rightY) / 2;

    const eyeDistance = Math.sqrt(
        Math.pow(rightX - leftX, 2) +
        Math.pow(rightY - leftY, 2)
    );

    const angle = Math.atan2(rightY - leftY, rightX - leftX);

    const glassesWidth = eyeDistance * TRYON_SCALE;
    const ratio = selectedGlassesImage.height / selectedGlassesImage.width;
    const glassesHeight = glassesWidth * ratio;

    const yOffset = glassesHeight * TRYON_Y_OFFSET_RATIO;

    tryonCtx.save();

    tryonCtx.translate(centerX, centerY + yOffset);
    tryonCtx.rotate(angle + Math.PI);

    tryonCtx.drawImage(
        selectedGlassesImage,
        -glassesWidth / 2,
        -glassesHeight / 2,
        glassesWidth,
        glassesHeight
    );

    tryonCtx.restore();
}

function selectGlasses(button) {
    const src = button.getAttribute("data-glass");

    if (!src) return;

    selectedGlassesImage = new Image();
    selectedGlassesImage.onload = function () {
        setTryonStatus("Đã chọn mẫu kính", "Bật camera để xem kính hiển thị trên khuôn mặt.");
    };
    selectedGlassesImage.src = src;

    const allButtons = document.querySelectorAll(".tryon-glass-item");
    allButtons.forEach(function (item) {
        item.classList.remove("active");
    });

    button.classList.add("active");
}

function updateTryonCameraButton(isOn) {
    const btn = document.getElementById("btnTryonCamera");
    if (!btn) return;

    if (isOn) {
        btn.innerHTML = `<i class="fa fa-stop"></i> Tắt camera`;
        btn.classList.add("is-active");
    } else {
        btn.innerHTML = `<i class="fa fa-video-camera"></i> Bật camera`;
        btn.classList.remove("is-active");
    }
}

function setTryonStatus(title, desc) {
    const titleEl = document.getElementById("tryonStatusTitle");
    const descEl = document.getElementById("tryonStatusDesc");

    if (titleEl) titleEl.innerText = title;
    if (descEl) descEl.innerText = desc;
}

function hideTryonPlaceholder() {
    const el = document.getElementById("tryonPlaceholder");
    if (el) el.style.display = "none";
}

function showTryonPlaceholder() {
    const el = document.getElementById("tryonPlaceholder");
    if (el) el.style.display = "flex";
}