document.addEventListener("DOMContentLoaded", function () {
    const registrationForm = document.getElementById("pharmacyRegisterForm");

    if (registrationForm) {
        registrationForm.addEventListener("submit", function (e) {
            let isValid = true;

            // Clear previous errors
            document.getElementById("domainError").style.display = "none";
            document.getElementById("passwordError").style.display = "none";

            // Check Pharmacy Domain Matching
            if(!checkPharmacyDomain()) {
                document.getElementById("domainError").innerText = "Manager email domain must match the Pharmacy Email Domain.";
                document.getElementById("domainError").style.display = "block";
                isValid = false;
            }

            // 2. Client-side Password Strength Check (Fallback)
            const password = document.getElementById("Password").value;
            const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$/;
            if(!passwordRegex.test(password)) {
                document.getElementById("passwordError").innerText = "Password must be at least 8 characters and include a symbol, number, uppercase and lowercase letter.";
                document.getElementById("passwordError").style.display = "block";
                isValid = false;
            }

            if(!isValid) {
                e.preventDefault(); // Prevent form submission if validation fails
            }
        });
    }
});

// Custom validation method implementation
function checkPharmacyDomain() {
    const pharmacyDomainInput = document.getElementById("PharmacyEmailDomain").value.trim();
    const managerEmailInput = document.getElementById("ManagerAuthenticationEmail").value.trim();

    if (pharmacyDomainInput && managerEmailInput) {
        const emailParts = managerEmailInput.split('@');
        if (emailParts.length === 2)
        {
            const managerDomain = emailParts[1];
            // Match the domain (case-insensitive)
            if (managerDomain.toLowerCase() === pharmacyDomainInput.toLowerCase()) {
                return true;
            }
        }
    }
    return false;
}