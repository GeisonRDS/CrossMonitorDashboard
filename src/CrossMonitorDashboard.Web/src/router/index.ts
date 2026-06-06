import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardView },
    { path: '/nodes/:id', name: 'details', component: () => import('../views/DetailsView.vue') },
    { path: '/settings', name: 'settings', component: () => import('../views/SettingsView.vue') },
    { path: '/editor', name: 'editor', component: () => import('../views/JsonEditorView.vue') },
    { path: '/about', name: 'about', component: () => import('../views/AboutView.vue') }
  ]
})

export default router
