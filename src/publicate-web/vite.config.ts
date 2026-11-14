import {defineConfig} from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import instance from '../../instance.json' with {type:'json'};
export default defineConfig({plugins:[react(),tailwindcss()],server:{port:instance.frontendPort,strictPort:true,allowedHosts:[new URL(instance.publicUrl).hostname],proxy:{'/api':{target:`http://localhost:${instance.backendPort}`,changeOrigin:false},'/media':{target:`http://localhost:${instance.backendPort}`,changeOrigin:false}}}});
