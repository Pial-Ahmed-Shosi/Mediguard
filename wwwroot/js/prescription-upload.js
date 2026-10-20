(() => {
    "use strict";

    const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
    const ACCEPTED_TYPES = ["image/jpeg", "image/png", "application/pdf"];
    const ACCEPTED_EXTS  = [".jpg", ".jpeg", ".png", ".pdf"];

    const dropzone    = document.getElementById("rxDropzone");
    const fileInput   = document.getElementById("rxFileInput");
    const previewBox  = document.getElementById("rxPreviewBox");
    const thumbContainer = document.getElementById("rxThumbContainer");
    const fileNameEl  = document.getElementById("rxFileName");
    const fileSizeEl  = document.getElementById("rxFileSize");
    const removeBtn   = document.getElementById("rxRemoveBtn");
    const fileError   = document.getElementById("rxFileError");
    const submitBtn   = document.getElementById("rxSubmitBtn");

    if (!dropzone || !fileInput) return;

    // ─── Utility ─────────────────────────────────────────────────────
    function formatSize(bytes) {
        if (bytes < 1024)       return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
    }

    function showError(msg) {
        fileError.textContent = msg;
        fileError.style.display = "block";
        submitBtn.disabled = true;
        clearPreview();
    }

    function clearError() {
        fileError.textContent = "";
        fileError.style.display = "none";
    }

    function clearPreview() {
        previewBox.classList.remove("visible");
        thumbContainer.innerHTML = "";
        fileNameEl.textContent = "—";
        fileSizeEl.textContent = "—";
    }

    // ─── Validation ───────────────────────────────────────────────────
    function validateFile(file) {
        if (!file) {
            showError("No file selected.");
            return false;
        }

        // Check extension
        const ext = "." + file.name.split(".").pop().toLowerCase();
        if (!ACCEPTED_EXTS.includes(ext)) {
            showError(`Invalid file type "${ext}". Accepted formats: JPG, PNG, PDF.`);
            return false;
        }

        // Check MIME type
        if (!ACCEPTED_TYPES.includes(file.type)) {
            showError(`Unsupported MIME type "${file.type}". Accepted: image/jpeg, image/png, application/pdf.`);
            return false;
        }

        // Check size: file.size <= 5 * 1024 * 1024
        if (file.size > MAX_FILE_SIZE) {
            showError(`File is too large (${formatSize(file.size)}). Maximum allowed size is 5 MB.`);
            return false;
        }

        return true;
    }

    // ─── Preview Rendering ────────────────────────────────────────────
    function renderPreview(file) {
        clearError();
        thumbContainer.innerHTML = "";

        const isPdf = file.type === "application/pdf";

        if (isPdf) {
            const icon = document.createElement("div");
            icon.className = "rx-pdf-thumb";
            icon.innerHTML = '<i class="bi bi-file-earmark-pdf-fill"></i>';
            thumbContainer.appendChild(icon);
        } else {
            const img = document.createElement("img");
            img.className = "rx-preview-thumb";
            img.alt = "Prescription preview";
            const reader = new FileReader();
            reader.onload = e => { img.src = e.target.result; };
            reader.readAsDataURL(file);
            thumbContainer.appendChild(img);
        }

        fileNameEl.textContent = file.name;
        fileSizeEl.textContent = formatSize(file.size);
        previewBox.classList.add("visible");
        submitBtn.disabled = false;
    }

    // ─── Handle file selection ────────────────────────────────────────
    function handleFile(file) {
        if (!validateFile(file)) return;
        renderPreview(file);
    }

    fileInput.addEventListener("change", () => {
        const file = fileInput.files?.[0];
        if (file) handleFile(file);
    });

    // ─── Drag & Drop ─────────────────────────────────────────────────
    dropzone.addEventListener("dragover", e => {
        e.preventDefault();
        dropzone.classList.add("drag-over");
    });

    dropzone.addEventListener("dragleave", () => {
        dropzone.classList.remove("drag-over");
    });

    dropzone.addEventListener("drop", e => {
        e.preventDefault();
        dropzone.classList.remove("drag-over");
        const file = e.dataTransfer?.files?.[0];
        if (file) {
            // Sync to input for form submission
            const dt = new DataTransfer();
            dt.items.add(file);
            fileInput.files = dt.files;
            handleFile(file);
        }
    });

    // ─── Remove ───────────────────────────────────────────────────────
    removeBtn?.addEventListener("click", () => {
        fileInput.value = "";
        clearPreview();
        clearError();
        submitBtn.disabled = true;
    });

    // ─── Prevent form submission if no file ──────────────────────────
    const form = document.getElementById("rxUploadForm");
    form?.addEventListener("submit", e => {
        if (!fileInput.files?.length) {
            e.preventDefault();
            showError("Please select a prescription file before submitting.");
            return;
        }
        if (!validateFile(fileInput.files[0])) {
            e.preventDefault();
        }
    });
})();
