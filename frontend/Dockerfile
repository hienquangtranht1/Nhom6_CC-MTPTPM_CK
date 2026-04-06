FROM nginx:alpine
# Copy the frontend files to the Nginx html directory
COPY index.html /usr/share/nginx/html/index.html
# Expose port 80 for the frontend
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
