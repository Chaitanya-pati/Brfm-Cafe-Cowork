document.addEventListener('DOMContentLoaded', function() {
    const navToggle = document.querySelector('.nav-toggle');
    const navMenu = document.querySelector('.nav-menu');

    // Create and insert overlay
    const overlay = document.createElement('div');
    overlay.className = 'nav-overlay';
    document.body.appendChild(overlay);

    function openMenu() {
        navMenu.classList.add('active');
        navToggle.classList.add('active');
        overlay.classList.add('active');
        document.body.classList.add('menu-open');
    }

    function closeMenu() {
        navMenu.classList.remove('active');
        navToggle.classList.remove('active');
        overlay.classList.remove('active');
        document.body.classList.remove('menu-open');
    }

    if (navToggle && navMenu) {
        navToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            if (navMenu.classList.contains('active')) {
                closeMenu();
            } else {
                openMenu();
            }
        });

        overlay.addEventListener('click', closeMenu);
        overlay.addEventListener('touchend', function(e) {
            e.preventDefault();
            closeMenu();
        });

        document.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', closeMenu);
        });

        // Close on Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') closeMenu();
        });
    }
    
    const header = document.querySelector('.header');
    let lastScroll = 0;
    
    window.addEventListener('scroll', function() {
        const currentScroll = window.pageYOffset;
        
        if (currentScroll > 50) {
            header.style.background = 'rgba(0, 0, 0, 0.8)';
        } else {
            header.style.background = 'rgba(255, 255, 255, 0.25)';
        }
        
        lastScroll = currentScroll;
    });
    
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);
    
    document.querySelectorAll('.page-inner').forEach(el => {
        observer.observe(el);
    });
    
    const scrollObserverOptions = {
        threshold: 0.15,
        rootMargin: '0px 0px -80px 0px'
    };
    
    const scrollObserver = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                
                if (entry.target.classList.contains('menu-category') ||
                    entry.target.classList.contains('amenity-card') ||
                    entry.target.classList.contains('pricing-card')) {
                    const siblings = entry.target.parentElement.children;
                    const index = Array.from(siblings).indexOf(entry.target);
                    entry.target.style.animationDelay = `${index * 0.1}s`;
                }
            }
        });
    }, scrollObserverOptions);
    
    const animatableElements = document.querySelectorAll(
        '.section-header, .menu-category, .amenity-card, .pricing-card, .hours-card'
    );
    
    animatableElements.forEach(el => {
        el.classList.add('scroll-animate');
        scrollObserver.observe(el);
    });
    
    document.querySelectorAll('.btn').forEach(btn => {
        btn.addEventListener('mouseenter', function(e) {
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            this.style.setProperty('--ripple-x', x + 'px');
            this.style.setProperty('--ripple-y', y + 'px');
        });
    });
    
    const pages = document.querySelectorAll('.page');
    
    pages.forEach(page => {
        page.addEventListener('mousemove', function(e) {
            const rect = this.getBoundingClientRect();
            const x = (e.clientX - rect.left) / rect.width;
            const y = (e.clientY - rect.top) / rect.height;
            
            const rotateX = (y - 0.5) * 5;
            const rotateY = (x - 0.5) * 5;
            
            if (window.innerWidth > 900) {
                this.style.transform = `perspective(1000px) rotateX(${-rotateX}deg) rotateY(${rotateY}deg) scale(1.02)`;
            }
        });
        
        page.addEventListener('mouseleave', function() {
            this.style.transform = '';
        });
    });
    
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
    
    const menuItems = document.querySelectorAll('.menu-items li');
    menuItems.forEach((item, index) => {
        item.style.transitionDelay = `${index * 0.05}s`;
    });
    
    const pricingFeatures = document.querySelectorAll('.pricing-features li');
    pricingFeatures.forEach((item, index) => {
        item.style.transitionDelay = `${index * 0.05}s`;
    });
});
