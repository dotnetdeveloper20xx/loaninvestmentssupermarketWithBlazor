/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.razor",
        "./**/*.html",
        "./**/*.cshtml"
    ],

    theme: {
        extend: {
            colors: {
                sidebar: {
                    900: "#071A2F",
                    800: "#0B2545",
                    700: "#103A66"
                }
            }
        }
    },

    plugins: [
        require("daisyui")
    ],

    daisyui: {
        themes: ["corporate"],
    }
}