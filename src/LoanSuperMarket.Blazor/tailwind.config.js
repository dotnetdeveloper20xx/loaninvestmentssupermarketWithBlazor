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
        themes: [
            {
                loansupermarket: {
                    primary: "#2563EB",
                    secondary: "#0EA5E9",
                    accent: "#14B8A6",
                    neutral: "#0F172A",
                    "base-100": "#FFFFFF",
                    "base-200": "#F5F7FA",
                    "base-300": "#E5E7EB",
                    info: "#2563EB",
                    success: "#16A34A",
                    warning: "#F59E0B",
                    error: "#DC2626"
                }
            }
        ]
    }
}