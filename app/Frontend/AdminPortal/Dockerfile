FROM node:20-alpine AS build

WORKDIR /app

# Install dependencies first for better layer caching
COPY package.json package-lock.json ./
RUN npm ci

# Copy application source
COPY . .

# Angular build configuration
ARG CONFIGURATION=production
ARG BASE_HREF=/

RUN npx ng build \
    --configuration="${CONFIGURATION}" \
    --base-href="${BASE_HREF}"


FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

# Custom SPA configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Angular 18 application builder outputs the browser bundle
COPY --from=build /app/dist/*/browser /usr/share/nginx/html

EXPOSE 8080