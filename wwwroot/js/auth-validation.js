document.addEventListener('DOMContentLoaded', function () {
    const forbiddenPublicDomains = ['gmail.com', 'yahoo.com', 'hotmail.com', 'outlook.com', 'icloud.com'];

    // Password Toggles
    const toggleButtons = document.querySelectorAll('.toggle-password');
    toggleButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');

            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.replace('bi-eye', 'bi-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.replace('bi-eye-slash', 'bi-eye');
            }
        });
    });

    // Dynamic Role Form Switching (Registration)
    const roleSelectors = document.querySelectorAll('.role-card-input');
    const roleSpecificSections = document.querySelectorAll('.role-specific-section');

    function updateDynamicFields() {
        const selectedRole = document.querySelector('.role-card-input:checked');
        if (!selectedRole) return;

        // Hide all specific sections
        roleSpecificSections.forEach(section => {
            section.classList.add('d-none');
            // Remove required attribute from hidden fields to allow form submission
            const inputs = section.querySelectorAll('input, select');
            inputs.forEach(input => input.removeAttribute('required'));
        });

        // Show targeted section
        const targetSectionId = `fields-${selectedRole.value.toLowerCase().replace(' ', '-')}`;
        const activeSection = document.getElementById(targetSectionId);

        if (activeSection) {
            activeSection.classList.remove('d-none');
            // Add required attribute to visible specific fields
            const inputs = activeSection.querySelectorAll('input, select');
            inputs.forEach(input => input.setAttribute('required', 'required'));
        }

        validateEmailDomain(); // Re-run validation on role change
    }

    roleSelectors.forEach(radio => {
        radio.addEventListener('change', updateDynamicFields);
    });

    // Email Domain Validation
    const emailInput = document.getElementById('Email');
    const authForm = document.getElementById('authForm');

    function validateEmailDomain() {
        if (!emailInput) return true;

        // Find role from either register cards or login hidden/select input
        let role = "Regular User";
        const selectedRoleInput = document.querySelector('.role-card-input:checked') || document.querySelector('input[name="Role"]:checked');
        if (selectedRoleInput) role = selectedRoleInput.value;

        const email = emailInput.value.trim().toLowerCase();
        const emailErrorBlock = document.getElementById('emailDomainFeedback');

        if (!email || !email.includes('@')) {
            clearEmailError();
            return true;
        }

        const domain = email.split('@')[1];

        if (role === 'Pharmacy Manager' && forbiddenPublicDomains.includes(domain)) {
            showEmailError("Managers must use an official pharmacy domain email (e.g., manager@citypharmacy.com).");
            return false;
        }

        clearEmailError();
        return true;
    }

    function showEmailError(msg) {
        emailInput.classList.add('is-invalid');
        const feedback = document.getElementById('emailDomainFeedback');
        if (feedback) {
            feedback.innerText = msg;
            feedback.style.display = 'block';
        }
    }

    function clearEmailError() {
        emailInput.classList.remove('is-invalid');
        const feedback = document.getElementById('emailDomainFeedback');
        if (feedback) {
            feedback.style.display = 'none';
        }
    }

    if (emailInput) {
        emailInput.addEventListener('input', validateEmailDomain);
    }

    if (authForm) {
        authForm.addEventListener('submit', function (e) {
            if (!validateEmailDomain()) {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    }

    // Initialize dynamic fields on load
    updateDynamicFields();
});