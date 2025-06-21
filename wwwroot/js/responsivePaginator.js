window.initResponsivePaginator = function ({
    data,
    tableColumns,
    tableWrapperId,
    accordionWrapperId,
    paginationWrapperId,
    pageSize = 10,
    renderRowButtons = null,
    accordionHeaderRender = null
}) {
    let currentPage = 1;

    function initTableHeader() {
        const table = document.getElementById(tableWrapperId);
        if (!table) return;

        if (!table.querySelector('thead')) {
            const thead = document.createElement('thead');
            const tr = document.createElement('tr');

            for (const col of tableColumns) {
                const th = document.createElement('th');
                th.textContent = col.title;
                if (col.className) th.className = col.className;
                tr.appendChild(th);
            }

            if (renderRowButtons) {
                const th = document.createElement('th');
                th.textContent = '操作';
                tr.appendChild(th);
            }

            thead.appendChild(tr);
            table.appendChild(thead);
        }

        if (!table.querySelector('tbody')) {
            table.appendChild(document.createElement('tbody'));
        }
    }

    function renderPaginationUI(totalPages) {
        const wrapper = document.getElementById(paginationWrapperId);
        if (!wrapper) return;

        const totalCount = data.length;
        const maxVisible = 5;
        let start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        let end = start + maxVisible - 1;
        if (end > totalPages) {
            end = totalPages;
            start = Math.max(1, end - maxVisible + 1);
        }

        let html = `<div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div>共 <span class="text-primary fw-bold">${totalCount}</span> 筆</div>
        <ul class="pagination pagination-sm mb-0">`;

        html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">&laquo;</a>
             </li>`;

        for (let i = start; i <= end; i++) {
            html += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                 </li>`;
        }

        html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">&raquo;</a>
             </li>`;

        html += `</ul></div>`;
        wrapper.innerHTML = html;

        wrapper.querySelectorAll('.page-link').forEach(link => {
            link.addEventListener('click', e => {
                e.preventDefault();
                const targetPage = parseInt(e.target.dataset.page);
                if (!isNaN(targetPage) && targetPage >= 1 && targetPage <= totalPages) {
                    currentPage = targetPage;
                    render();
                }
            });
        });
    }

    function render() {
        initTableHeader();

        const start = (currentPage - 1) * pageSize;
        const pageData = data.slice(start, start + pageSize);

        // 🖥️ 桌機 Table
        const table = document.getElementById(tableWrapperId);
        if (table) {
            const tbody = table.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = pageData.map(row => {
                    const cells = tableColumns.map(col => {
                        const tdClass = col.className ? ` class="${col.className}"` : '';
                        return `<td${tdClass}>${col.render(row)}</td>`;
                    }).join('');
                    const buttons = renderRowButtons ? `<td>${renderRowButtons(row)}</td>` : '';
                    return `<tr>${cells}${buttons}</tr>`;
                }).join('');
            }
        }

        // 📱 手機 Accordion
        const accordion = document.getElementById(accordionWrapperId);
        if (accordion) {
            accordion.innerHTML = pageData.map((row, index) => {
                const body = tableColumns.map(col => {
                    const divClass = col.className ? ` class="${col.className}"` : '';
                    return `<div${divClass}><strong>${col.title}：</strong>${col.render(row)}</div>`;
                }).join('');
                const buttons = renderRowButtons ? `<div class="mt-2">${renderRowButtons(row)}</div>` : '';
                const headerText = accordionHeaderRender ? accordionHeaderRender(row) : tableColumns[0].render(row);

                return `
            <div class="accordion-item">
                <h2 class="accordion-header" id="heading-${index}">
                    <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${index}">
                        ${headerText}
                    </button>
                </h2>
                <div id="collapse-${index}" class="accordion-collapse collapse" data-bs-parent="#${accordionWrapperId}">
                    <div class="accordion-body">${body}${buttons}</div>
                </div>
            </div>`;
            }).join('');
        }

        const totalPages = Math.ceil(data.length / pageSize);
        renderPaginationUI(totalPages);
    }

    function handleResponsiveView() {
        function updateView() {
            const isMobile = window.innerWidth < 768;

            const tableElement = document.getElementById(tableWrapperId);
            const accordionElement = document.getElementById(accordionWrapperId);
            console.log(accordionElement);
            const tableWrapper = tableElement?.closest(".table-responsive");
            const accordionWrapper = accordionElement; // 👈 直接本體

            if (tableWrapper && accordionWrapper) {
                tableWrapper.style.display = isMobile ? "none" : "block";
                accordionWrapper.style.display = isMobile ? "block" : "none";
            }
        }

        updateView();
        window.addEventListener("resize", updateView);
    }

    handleResponsiveView();
    render();
};
