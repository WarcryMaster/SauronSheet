module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './Shared/**/*.cshtml',
    './wwwroot/js/**/*.js'
  ],
  theme: {
    extend: {
      colors: {
        primary: '#2563eb',
        secondary: '#64748b',
        danger: '#dc2626',
        success: '#16a34a',
        warning: '#f59e42'
      }
    }
  },
  plugins: [],
  darkMode: 'class',
}
