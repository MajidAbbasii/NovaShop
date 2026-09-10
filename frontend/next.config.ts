import path from 'path';

const nextConfig = {
  images: {
    unoptimized: true,
    remotePatterns: [
      { protocol: 'https', hostname: 'picsum.photos' },
      { protocol: 'https', hostname: '*.picsum.photos' },
      { protocol: 'http', hostname: 'localhost', port: '5250' },
      { protocol: 'https', hostname: 'localhost' },
      { protocol: 'http', hostname: '*', port: '5250' },
      { protocol: 'https', hostname: '*' },
    ],
  },

  turbopack: {
    root: path.resolve(__dirname),
  },
};

export default nextConfig;
