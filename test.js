// Translated code by SyntaxFlow Blockly-to-JavaScript engine
// Helper methods are auto-included as needed for block compatibility

function isPrime(n) {
    if (n <= 1) return false;
    if (n === 2) return true;
    if (n % 2 === 0) return false;
    for (let i = 3; i <= Math.sqrt(n); i += 2) {
        if (n % i === 0) return false;
    }
    return true;
}

// --- End of helpers, user code below ---

let input = 5;
console.log((isPrime(input) ? String(input) + " is a prime number" : String(input) + " is not a prime number"));