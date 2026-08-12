import { createApp } from 'vue'
import { createPinia } from 'pinia'
import dayjs from 'dayjs'
import jalaliday from 'jalaliday'
import 'dayjs/locale/fa'
import App from './App.vue'
import router from './router'
import { useThemeStore } from './stores/theme'
import { useAuthStore } from './stores/auth'
import { initPwaInstallListeners } from './composables/usePwaInstall'
import './style.css'

dayjs.extend(jalaliday)
dayjs.locale('fa')

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const theme = useThemeStore()
theme.init()
initPwaInstallListeners()

const auth = useAuthStore()
auth.loadFromStorage()
if (auth.isAuthenticated) {
  auth.fetchProfile().catch(() => {})
}

app.mount('#app')
