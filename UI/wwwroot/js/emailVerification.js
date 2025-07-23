// Email verification UI logic for Home/Index.cshtml
// Handles notification, modal, API calls, and error display

(function() {
    let emailVerified = false;
    let userEmail = '';
    let isRequesting = false;
    let verificationModal = null;

    // Utility: Show notification if not verified
    function showEmailVerificationNotice() {
        if (!emailVerified) {
            const notice = document.createElement('div');
            notice.className = 'alert alert-warning alert-dismissible fade show';
            notice.id = 'emailVerifyNotice';
            notice.innerHTML = `
                <strong>Email not verified!</strong> Please verify your email to unlock all features.
                <button class="btn btn-sm btn-primary ms-2" id="openVerifyModalBtn">Verify Email</button>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;
            document.querySelector('.container').prepend(notice);
            document.getElementById('openVerifyModalBtn').onclick = showVerificationModal;
        }
    }

    // Utility: Show modal for verification
    function showVerificationModal() {
        if (!verificationModal) {
            verificationModal = createVerificationModal();
            document.body.appendChild(verificationModal);
        }
        const modal = new bootstrap.Modal(verificationModal);
        modal.show();
    }

    // Create modal HTML
    function createVerificationModal() {
        const modalDiv = document.createElement('div');
        modalDiv.className = 'modal fade';
        modalDiv.id = 'emailVerifyModal';
        modalDiv.tabIndex = -1;
        modalDiv.setAttribute('aria-labelledby', 'emailVerifyModalLabel');
        modalDiv.setAttribute('aria-hidden', 'true');
        modalDiv.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="emailVerifyModalLabel">Email Verification</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <form id="emailVerifyForm" autocomplete="off">
                        <div class="mb-3">
                            <label for="verifyEmail" class="form-label">Email</label>
                            <input type="email" class="form-control" id="verifyEmail" value="" required />
                        </div>
                        <div class="mb-3">
                            <button type="button" class="btn btn-outline-primary w-100" id="sendVerifyEmailBtn">Send Verification Email</button>
                        </div>
                        <div class="mb-3">
                            <label for="verifyOtp" class="form-label">Verification Code (OTP)</label>
                            <input type="text" class="form-control" id="verifyOtp" maxlength="6" required />
                        </div>
                        <div class="mb-3">
                            <button type="button" class="btn btn-success w-100" id="submitVerifyBtn">Verify</button>
                        </div>
                        <div class="mb-2">
                            <span id="verifyError" class="text-danger"></span>
                            <span id="verifySuccess" class="text-success"></span>
                        </div>
                    </form>
                </div>
            </div>
        </div>
        `;
        // Attach event listeners
        setTimeout(() => {
            document.getElementById('sendVerifyEmailBtn').onclick = sendVerificationEmail;
            document.getElementById('submitVerifyBtn').onclick = submitVerificationOtp;
        }, 100);
        return modalDiv;
    }

    // Send verification email
    async function sendVerificationEmail() {
        if (isRequesting) return;
        isRequesting = true;
        setVerifyStatus('', '');
        disableVerifyForm(true);
        const email = document.getElementById('verifyEmail').value;
        try {
            const res = await fetch(apiBaseUrl + 'api/auth/request-email-verification', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': getJwt() },
                body: JSON.stringify({ email })
            });
            const data = await res.json();
            if (res.ok) {
                setVerifyStatus('', 'Verification email sent. Please check your inbox.');
            } else {
                setVerifyStatus(data.message || 'Failed to send email.', '');
            }
        } catch (err) {
            setVerifyStatus('Network error.', '');
        }
        disableVerifyForm(false);
        isRequesting = false;
    }

    // Submit OTP for verification
    async function submitVerificationOtp() {
        if (isRequesting) return;
        isRequesting = true;
        setVerifyStatus('', '');
        disableVerifyForm(true);
        const email = document.getElementById('verifyEmail').value;
        const otp = document.getElementById('verifyOtp').value;
        try {
            const res = await fetch(apiBaseUrl + 'api/auth/verify-email-otp', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': getJwt() },
                body: JSON.stringify({ email, otp })
            });
            const data = await res.json();
            if (res.ok) {
                setVerifyStatus('', 'Email verified successfully!');
                emailVerified = true;
                setTimeout(() => {
                    document.getElementById('emailVerifyNotice')?.remove();
                    bootstrap.Modal.getInstance(document.getElementById('emailVerifyModal')).hide();
                }, 1500);
            } else {
                setVerifyStatus(data.message || 'Verification failed.', '');
            }
        } catch (err) {
            setVerifyStatus('Network error.', '');
        }
        disableVerifyForm(false);
        isRequesting = false;
    }

    // Helpers
    function setVerifyStatus(error, success) {
        document.getElementById('verifyError').textContent = error;
        document.getElementById('verifySuccess').textContent = success;
    }
    function disableVerifyForm(disabled) {
        document.getElementById('verifyEmail').disabled = disabled;
        document.getElementById('sendVerifyEmailBtn').disabled = disabled;
        document.getElementById('verifyOtp').disabled = disabled;
        document.getElementById('submitVerifyBtn').disabled = disabled;
    }
    function getJwt() {
        // Production: return JWT from localStorage
        const token = localStorage.getItem('token');
        return token ? `Bearer ${token}` : '';
    }

    // Entry: after login/register, set emailVerified and userEmail
    window.setEmailVerificationStatus = function(isVerified, email) {
        emailVerified = isVerified;
        userEmail = email;
        // Remove any previous notice
        document.getElementById('emailVerifyNotice')?.remove();
        if (!isVerified) showEmailVerificationNotice();
    };

    // Optionally, auto-fill email in modal
    document.addEventListener('shown.bs.modal', function(e) {
        if (e.target.id === 'emailVerifyModal') {
            document.getElementById('verifyEmail').value = userEmail || '';
        }
    });

    // Auto-inject notification on page load if needed
    window.addEventListener('DOMContentLoaded', function() {
        // Optionally, fetch user status from API if not set
        // For now, rely on login/register to call setEmailVerificationStatus
    });
})();
