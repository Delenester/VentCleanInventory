
// Format numbers with comma as decimal separator
function formatNumber(value) {
    if (value === null || value === undefined || value === '') return '';
    
    // If already a string with comma, return as is
    if (typeof value === 'string' && value.includes(',')) return value;
    
    // Convert to number and format
    var num = parseFloat(value);
    if (isNaN(num)) return value;
    
    // Format with comma as decimal separator
    return num.toString().replace('.', ',');
}

// Auto-format all numeric inputs on page load
document.addEventListener('DOMContentLoaded', function() {
    // Find all input fields that might contain numbers
    var numericInputs = document.querySelectorAll('input[type="number"], input.number-format');
    
    numericInputs.forEach(function(input) {
        // On blur, format the value with comma
        input.addEventListener('blur', function() {
            if (this.value && this.value.includes('.')) {
                this.value = this.value.replace('.', ',');
            }
        });
        
        // On focus, allow typing decimal point
        input.addEventListener('focus', function() {
            // Keep current value as is
        });
    });
});

// Toggle password visibility
function togglePasswordVisibility(inputId, iconId) {
    var input = document.getElementById(inputId);
    var icon = document.getElementById(iconId);
    if (input && icon) {
        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.remove('bi-eye');
            icon.classList.add('bi-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.remove('bi-eye-slash');
            icon.classList.add('bi-eye');
        }
    }
}