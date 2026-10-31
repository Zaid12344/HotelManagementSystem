// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function showSkeleton() {
    skeleton.style.display = 'flex';
    skeleton.style.opacity = '1';
    roomList.style.opacity = '0';
    roomList.style.pointerEvents = 'none';
}

function hideSkeleton() {
    skeleton.style.display = 'none';
    roomList.style.opacity = '1';
    roomList.style.pointerEvents = 'auto';
}