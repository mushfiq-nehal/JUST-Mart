// Modern JUST Mart - Interactive Features

document.addEventListener('DOMContentLoaded', function() {
    
    // Filter functionality
    const filterBtns = document.querySelectorAll('.filter-btn');
    
    filterBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            // Remove active class from all buttons
            filterBtns.forEach(b => b.classList.remove('active'));
            
            // Add active class to clicked button
            this.classList.add('active');
            
            // Add smooth animation
            const productsGrid = document.querySelector('.products-grid');
            productsGrid.style.opacity = '0';
            
            setTimeout(() => {
                productsGrid.style.opacity = '1';
            }, 200);
        });
    });
    
    // Wishlist functionality
    const wishlistBtns = document.querySelectorAll('.wishlist-btn');
    
    wishlistBtns.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const icon = this.querySelector('i');
            
            if (icon.classList.contains('bi-heart')) {
                icon.classList.remove('bi-heart');
                icon.classList.add('bi-heart-fill');
                this.style.background = '#ec4899';
                this.style.color = 'white';
                
                // Show toast notification
                showToast('Added to wishlist!', 'success');
            } else {
                icon.classList.remove('bi-heart-fill');
                icon.classList.add('bi-heart');
                this.style.background = 'white';
                this.style.color = '';
                
                showToast('Removed from wishlist', 'info');
            }
        });
    });
    
    // Quick view functionality
    const viewBtns = document.querySelectorAll('.view-btn');
    
    viewBtns.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            showToast('Quick view feature coming soon!', 'info');
        });
    });
    
    // Compare functionality
    const compareBtns = document.querySelectorAll('.compare-btn');
    
    compareBtns.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            showToast('Compare feature coming soon!', 'info');
        });
    });
    
    // Smooth scroll to products
    const startShoppingBtn = document.querySelector('a[href="#products"]');
    if (startShoppingBtn) {
        startShoppingBtn.addEventListener('click', function(e) {
            e.preventDefault();
            document.querySelector('#products').scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        });
    }
    
    // Add to cart animation
    const addCartBtns = document.querySelectorAll('.btn-add-cart');
    
    addCartBtns.forEach(btn => {
        btn.addEventListener('click', function(e) {
            // Create ripple effect
            const ripple = document.createElement('span');
            ripple.classList.add('ripple');
            this.appendChild(ripple);
            
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });
});

// Toast notification function
function showToast(message, type = 'info') {
    // Remove existing toast if any
    const existingToast = document.querySelector('.custom-toast');
    if (existingToast) {
        existingToast.remove();
    }
    
    // Create toast element
    const toast = document.createElement('div');
    toast.classList.add('custom-toast', `toast-${type}`);
    toast.innerHTML = `
        <div class="toast-content">
            <i class="bi bi-check-circle-fill me-2"></i>
            <span>${message}</span>
        </div>
    `;
    
    document.body.appendChild(toast);
    
    // Trigger animation
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);
    
    // Remove toast after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, 3000);
}

// Add ripple effect CSS dynamically
const style = document.createElement('style');
style.textContent = `
    .ripple {
        position: absolute;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.6);
        width: 100px;
        height: 100px;
        margin-top: -50px;
        margin-left: -50px;
        animation: ripple-animation 0.6s ease-out;
        pointer-events: none;
    }
    
    @keyframes ripple-animation {
        from {
            opacity: 1;
            transform: scale(0);
        }
        to {
            opacity: 0;
            transform: scale(2);
        }
    }
    
    .custom-toast {
        position: fixed;
        top: 100px;
        right: 20px;
        background: white;
        padding: 1rem 1.5rem;
        border-radius: 12px;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
        transform: translateX(400px);
        transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        z-index: 9999;
        min-width: 250px;
    }
    
    .custom-toast.show {
        transform: translateX(0);
    }
    
    .toast-content {
        display: flex;
        align-items: center;
        font-weight: 600;
        color: #1f2937;
    }
    
    .toast-success .bi {
        color: #10b981;
    }
    
    .toast-info .bi {
        color: #6366f1;
    }
    
    .toast-warning .bi {
        color: #f59e0b;
    }
    
    .toast-error .bi {
        color: #ef4444;
    }
`;
document.head.appendChild(style);
