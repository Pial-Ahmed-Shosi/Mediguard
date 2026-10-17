document.addEventListener("DOMContentLoaded", function () {
    // Resolve elements with fallbacks for maximum view compatibility
    const dropZone = document.getElementById("drop-zone");
    const fileInput = document.getElementById("prescriptionFileInput") || document.getElementById("PrescriptionFile");
    const btnBrowse = document.getElementById("btn-browse-file");
    const btnRemove = document.getElementById("btn-remove-file") || document.getElementById("remove-file-btn");
    const btnSubmit = document.getElementById("btn-submit-prescription") || document.getElementById("submit-upload-btn");
    const fileError = document.getElementById("file-error-message") || document.getElementById("file-validation-error");

    const previewContainer = document.getElementById("preview-container");
    const imagePreviewWrapper = document.getElementById("image-preview-wrapper");
    const imagePreview = document.getElementById("image-preview");
    const pdfPreviewWrapper = document.getElementById("pdf-preview-wrapper") || document.getElementById("pdf-preview");
    const pdfFilename = document.getElementById("pdf-filename") || document.getElementById("pdf-file-name");
    const pdfFilesize = document.getElementById("pdf-filesize") || document.getElementById("pdf-file-size");

    const MAX_FILE_SIZE = 5 * 1024 * 1024; // Exactly 5 MB (5,242,880 bytes)
    const ACCEPTED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".pdf"];
    const ACCEPTED_MIME_TYPES = ["image/jpeg", "image/png", "application/pdf"];

    if (!dropZone || !fileInput) return;

    // 1. Prevent Default Drag Behaviors
    ["dragenter", "dragover", "dragleave", "drop"].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // 2. Drag Hover Highlights
    ["dragenter", "dragover"].forEach(eventName => {
        dropZone.addEventListener(eventName, function () {
            dropZone.classList.add("dragover");
            dropZone.classList.add("drop-zone-active");
        }, false);
    });

    ["dragleave", "drop"].forEach(eventName => {
        dropZone.addEventListener(eventName, function () {
            dropZone.classList.remove("dragover");
            dropZone.classList.remove("drop-zone-active");
        }, false);
    });

    // 3. Handle Dropped Files
    dropZone.addEventListener("drop", function (e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        if (files && files.length > 0) {
            fileInput.files = files;
            handleFileSelection(files[0]);
        }
    });

    // 4. Click-to-Browse Event Handling
    if (btnBrowse) {
        btnBrowse.addEventListener("click", function (e) {
            e.stopPropagation();
            fileInput.click();
        });
    }

    dropZone.addEventListener("click", function (e) {
        if (btnBrowse && e.target === btnBrowse) return;
        if (btnRemove && e.target === btnRemove) return;
        fileInput.click();
    });

    fileInput.addEventListener("change", function () {
        if (fileInput.files && fileInput.files.length > 0) {
            handleFileSelection(fileInput.files[0]);
        } else {
            clearSelection();
        }
    });

    // 5. Remove Selected File Action
    if (btnRemove) {
        btnRemove.addEventListener("click", function (e) {
            e.stopPropagation();
            fileInput.value = "";
            clearSelection();
        });
    }

    // 6. File Admissibility & Validation Function
    function handleFileSelection(file) {
        hideError();

        if (!file) {
            clearSelection();
            return;
        }

        const fileName = file.name.toLowerCase();
        const fileExtension = "." + fileName.split('.').pop();
        const fileType = file.type;

        // Verify Extension & MIME Type
        const isValidExtension = ACCEPTED_EXTENSIONS.some(ext => fileName.endsWith(ext));
        const isValidMime = ACCEPTED_MIME_TYPES.includes(fileType) || fileType === "";

        if (!isValidExtension || !isValidMime) {
            showError("Invalid file type. Accepted formats: JPG, PNG, PDF.");
            clearSelection();
            return;
        }

        // Verify File Size boundary (file.size <= 5 * 1024 * 1024)
        if (file.size > MAX_FILE_SIZE) {
            showError("File size exceeds 5MB limit. Please select a smaller file.");
            clearSelection();
            return;
        }

        // Display File Preview
        renderPreview(file, fileExtension, fileType);
        if (btnSubmit) {
            btnSubmit.disabled = false;
        }
    }

    // 7. Render Live Document Preview
    function renderPreview(file, extension, type) {
        if (previewContainer) {
            previewContainer.classList.remove("d-none");
        }

        const isPdf = extension === ".pdf" || type === "application/pdf";

        if (isPdf) {
            if (imagePreviewWrapper) imagePreviewWrapper.classList.add("d-none");
            if (imagePreview) imagePreview.classList.add("d-none");

            if (pdfPreviewWrapper) pdfPreviewWrapper.classList.remove("d-none");
            if (pdfFilename) pdfFilename.textContent = file.name;
            if (pdfFilesize) pdfFilesize.textContent = formatBytes(file.size);
        } else {
            if (pdfPreviewWrapper) pdfPreviewWrapper.classList.add("d-none");

            if (imagePreviewWrapper) imagePreviewWrapper.classList.remove("d-none");
            if (imagePreview) imagePreview.classList.remove("d-none");

            const reader = new FileReader();
            reader.onload = function (e) {
                if (imagePreview) {
                    imagePreview.src = e.target.result;
                }
            };
            reader.readAsDataURL(file);
        }
    }

    // 8. Clear Selection State
    function clearSelection() {
        if (previewContainer) previewContainer.classList.add("d-none");
        if (imagePreviewWrapper) imagePreviewWrapper.classList.add("d-none");
        if (pdfPreviewWrapper) pdfPreviewWrapper.classList.add("d-none");
        if (imagePreview) {
            imagePreview.src = "#";
            imagePreview.classList.add("d-none");
        }
        if (pdfFilename) pdfFilename.textContent = "";
        if (pdfFilesize) pdfFilesize.textContent = "";
        if (btnSubmit) btnSubmit.disabled = true;
    }

    function showError(msg) {
        if (fileError) {
            fileError.textContent = msg;
            fileError.classList.remove("d-none");
        }
    }

    function hideError() {
        if (fileError) {
            fileError.textContent = "";
            fileError.classList.add("d-none");
        }
    }

    function formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
});