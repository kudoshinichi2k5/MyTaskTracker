FROM node:22-alpine AS build
WORKDIR /src

ARG CONFIGURATION=development
ARG BASE_HREF=/

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration="${CONFIGURATION}" --base-href="${BASE_HREF}"

FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime-base
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080

FROM runtime-base AS runtime
COPY --from=build /src/dist/admin-portal/browser/ /usr/share/nginx/html/

FROM runtime-base AS runtime-artifact
COPY dist/ /usr/share/nginx/html/