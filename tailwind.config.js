/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: "class",
  content: [
    "./Pages/**/*.cshtml",
    "./Areas/**/*.cshtml",
    "./Views/**/*.cshtml",
    "./wwwroot/**/*.js"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Sora", "ui-sans-serif", "system-ui"]
      },
      colors: {
        ink: "#0f172a",
        fog: "#e2e8f0",
        mist: "#f8fafc",
        accent: "#0f766e"
      }
    }
  },
  plugins: []
};
