const nextConfig = {
  images: {
    unoptimized: true,
    remotePatterns: [
      { protocol: 'https' as const, hostname: 'picsum.photos' },
      { protocol: 'https' as const, hostname: '*.picsum.photos' },
      { protocol: 'http' as const, hostname: 'localhost', port: '5100' },
      { protocol: 'https' as const, hostname: 'localhost' },
    ],
  },
};

export default nextConfig;
