document.addEventListener("DOMContentLoaded", function () {
    const dropZone = document.getElementById("drop-zone");
    const fileInput = document.getElementById("PrescriptionFile");
    const previewContainer = document.getElementById("preview-container");
    const imagePreview = document.getElementById("image-preview");
    const pdfPreview = document.getElementById("pdf-preview");
    const pdfFileName = document.getElementById("pdf-file-name");
    const pdfFileSize = document.getElementById("pdf-file-size");
    const removeFileBtn = document.getElementById("remove-file-btn");
    const validationError = document.getElementById("file-validation-error");
    const submitBtn = document.getElementById("submit-upload-btn");

    const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB limit
    const ALLOWED_MIME_TYPES = ["image/jpeg", "image/png", "application/pdf"];
    const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".pdf"];

    // Prevent default drag behaviors
    ["dragenter", "dragover", "dragleave", "drop"].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Highlight drop zone when dragging over
    ["dragenter", "dragover"].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.add("drop-zone-active"), false);
    });

    ["dragleave", "drop"].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.remove("drop-zone-active"), false);
    });

    // Handle dropped files
    dropZone.addEventListener("drop", function (e) {
        const dt = e.dataTransfer;
        const files = dt.files;

        if (files.length > 0) {
            fileInput.files = files;
            handleFileSelection(files[0]);
        }
    });

    // Handle click to select file
    fileInput.addEventListener("change", function () {
        if (this.files.length > 0) {
            handleFileSelection(this.files[0]);
        }
    });

    // Reset selection
    removeFileBtn.addEventListener("click", resetFileInput);

    function handleFileSelection(file) {
        clearError();

        // 1. Validate File Extension & MIME Type
        const fileName = file.name.toLowerCase();
        const isValidExtension = ALLOWED_EXTENSIONS.some(ext => fileName.endsWith(ext));
        const isValidMime = ALLOWED_MIME_TYPES.includes(file.type);

        if (!isValidExtension && !isValidMime) {
            showError("Invalid file type. Please upload a JPG, PNG, or PDF file.");
            resetFileInput();
            return;
        }

        // 2. Validate File Size (<= 5MB)
        if (file.size > MAX_FILE_SIZE) {
            showError("File size exceeds 5MB limit. Please upload a smaller file.");
            resetFileInput();
            return;
        }

        // 3. Render Live Preview
        previewContainer.classList.remove("d-none");
        dropZone.classList.add("d-none");
        submitBtn.disabled = false;

        if (file.type.startsWith("image/")) {
            const reader = new FileReader();
            reader.onload = function (e) {
                imagePreview.src = e.target.result;
                imagePreview.classList.remove("d-none");
                pdfPreview.classList.add("d-none");
            };
            reader.readAsDataURL(file);
        } else if (file.type === "application/pdf" || fileName.endsWith(".pdf")) {
            imagePreview.classList.add("d-none");
            pdfPreview.classList.remove("d-none");
            pdfFileName.textContent = file.name;
            pdfFileSize.textContent = (file.size / (1024 * 1024)).toFixed(2) + " MB";
        }
    }

    function resetFileInput() {
        fileInput.value = "";
        previewContainer.classList.add("d-none");
        dropZone.classList.remove("d-none");
        imagePreview.src = "";
        submitBtn.disabled = true;
        clearError();
    }

    function showError(message) {
        validationError.textContent = message;
        validationError.classList.remove("d-none");
    }

    function clearError() {
        validationError.textContent = "";
        validationError.classList.add("d-none");
    }
});