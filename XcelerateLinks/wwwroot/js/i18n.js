/**
 * XcelerateLinks – PT ↔ EN Internationalisation
 * Applies translations to elements that have a data-i18n attribute or
 * whose text matches a known phrase in the active language dictionary.
 */
(function (global) {
    'use strict';

    var translations = {
        pt: {
            // Nav / header
            'Início': 'Início',
            'Oportunidades': 'Oportunidades',
            'Mensagens': 'Mensagens',
            'Ligações': 'Ligações',
            'Rede': 'Rede',
            'Empresas': 'Empresas',
            'Candidaturas': 'Candidaturas',
            'O Meu Perfil': 'O Meu Perfil',
            'Sair': 'Sair',
            'Entrar': 'Entrar',
            'Criar Conta': 'Criar Conta',
            // Home hero
            'Conecte Talento com Oportunidades': 'Conecte Talento com Oportunidades',
            'Explorar Oportunidades': 'Explorar Oportunidades',
            'Ver Talentos': 'Ver Talentos',
            'Começar Agora': 'Começar Agora',
            // Stats
            'Utilizadores Ativos': 'Utilizadores Ativos',
            'Ligações Ativas': 'Ligações Ativas',
            // Features section
            'Por Que Escolher Xcelerate Links?': 'Por Que Escolher Xcelerate Links?',
            // Buttons
            '+ Ligar': '+ Ligar',
            'Conectado': 'Conectado',
            'Pendente': 'Pendente',
            // Chat
            'Sem mensagens': 'Sem mensagens',
            'Escreve uma mensagem...': 'Escreve uma mensagem...',
            'Pesquisar conversas...': 'Pesquisar conversas...',
            'Nova': 'Nova',
            // Profile
            'Editar Perfil': 'Editar Perfil',
            'Sobre Mim': 'Sobre Mim',
            'Experiência': 'Experiência',
            'Formação': 'Formação',
            'Competências': 'Competências',
            // Network
            'Explorar a Rede': 'Explorar a Rede',
            // Connections
            'As Minhas Ligações': 'As Minhas Ligações',
            'Pedidos pendentes': 'Pedidos pendentes',
            'Aceitar': 'Aceitar',
            'Recusar': 'Recusar',
            // Applications
            'Candidaturas Recentes': 'Candidaturas Recentes',
            // Footer
            'Sobre': 'Sobre',
            'Recursos': 'Recursos',
            'Legal': 'Legal',
            'Segue-nos': 'Segue-nos',
            'Privacidade': 'Privacidade',
            'Termos': 'Termos',
            'Contacto': 'Contacto',
            'Todos os direitos reservados.': 'Todos os direitos reservados.',
        },
        en: {
            // Nav / header
            'Início': 'Home',
            'Oportunidades': 'Opportunities',
            'Mensagens': 'Messages',
            'Ligações': 'Connections',
            'Rede': 'Network',
            'Empresas': 'Companies',
            'Candidaturas': 'Applications',
            'O Meu Perfil': 'My Profile',
            'Sair': 'Log out',
            'Entrar': 'Log in',
            'Criar Conta': 'Create Account',
            // Home hero
            'Conecte Talento com Oportunidades': 'Connect Talent with Opportunities',
            'Explorar Oportunidades': 'Explore Opportunities',
            'Ver Talentos': 'Browse Talent',
            'Começar Agora': 'Get Started',
            // Stats
            'Utilizadores Ativos': 'Active Users',
            'Ligações Ativas': 'Active Connections',
            // Features section
            'Por Que Escolher Xcelerate Links?': 'Why Choose Xcelerate Links?',
            // Buttons
            '+ Ligar': '+ Connect',
            'Conectado': 'Connected',
            'Pendente': 'Pending',
            // Chat
            'Sem mensagens': 'No messages',
            'Escreve uma mensagem...': 'Write a message...',
            'Pesquisar conversas...': 'Search conversations...',
            'Nova': 'New',
            // Profile
            'Editar Perfil': 'Edit Profile',
            'Sobre Mim': 'About',
            'Experiência': 'Experience',
            'Formação': 'Education',
            'Competências': 'Skills',
            // Network
            'Explorar a Rede': 'Explore Network',
            // Connections
            'As Minhas Ligações': 'My Connections',
            'Pedidos pendentes': 'Pending requests',
            'Aceitar': 'Accept',
            'Recusar': 'Decline',
            // Applications
            'Candidaturas Recentes': 'Recent Applications',
            // Footer
            'Sobre': 'About',
            'Recursos': 'Resources',
            'Legal': 'Legal',
            'Segue-nos': 'Follow us',
            'Privacidade': 'Privacy',
            'Termos': 'Terms',
            'Contacto': 'Contact',
            'Todos os direitos reservados.': 'All rights reserved.',
        }
    };

    /**
     * Walk the DOM and translate text nodes that match known phrases.
     * Also handles [data-i18n] attributes for explicit tagging.
     */
    function applyLang(lang) {
        var dict = translations[lang] || translations['pt'];

        // 1. Explicit data-i18n attributes
        document.querySelectorAll('[data-i18n]').forEach(function (el) {
            var key = el.getAttribute('data-i18n');
            if (dict[key]) el.textContent = dict[key];
        });

        // 2. data-i18n-placeholder for inputs
        document.querySelectorAll('[data-i18n-placeholder]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-placeholder');
            if (dict[key]) el.placeholder = dict[key];
        });

        // 3. Auto-translate known text nodes (nav links, buttons, headings)
        var selector = 'a.dropdown-item, .footer-link, .btn-cta, .btn-ghost:not(#themeToggle):not(#langToggle), nav a, footer h4, footer p, h1, h2, h3, h4, h5, .stat-label, .section-title, .section-subtitle, .hero-content h1, .hero-content p, .hero-actions a';
        document.querySelectorAll(selector).forEach(function (el) {
            // Only translate leaf text (no child elements)
            if (el.children.length === 0) {
                var text = el.textContent.trim();
                if (dict[text]) el.textContent = dict[text];
            }
        });

        // 4. Translate placeholder attributes
        document.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(function (el) {
            var ph = el.getAttribute('placeholder');
            if (ph && dict[ph]) el.setAttribute('placeholder', dict[ph]);
        });

        document.documentElement.lang = lang === 'en' ? 'en' : 'pt-PT';
    }

    global.XcelerateI18n = { apply: applyLang, translations: translations };

    // Auto-apply on DOMContentLoaded
    var savedLang = localStorage.getItem('siteLang') || 'pt';
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { applyLang(savedLang); });
    } else {
        applyLang(savedLang);
    }

})(window);
