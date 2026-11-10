document.addEventListener("DOMContentLoaded", () => {
    const page = document.getElementById("appointment-page");
    if (!page) return;

    const form = document.getElementById("appointment-filters");
    const body = document.getElementById("appointments-body");
    const search = document.getElementById("appointment-search");
    const error = document.getElementById("appointment-error");
    const pageLabel = document.getElementById("page-label");
    const previous = document.getElementById("page-previous");
    const next = document.getElementById("page-next");
    const pageSize = 10;
    let appointments = [];
    let currentPage = 1;
    let sortKey = "date";
    let sortDirection = 1;
    let activeRequest = null;
    function statusGroup(item) {
        const label = (item.status_name || "").toLocaleLowerCase().trim();
        if (label.includes("complete") || label.includes("заврш")) return "completed";
        if (label.includes("progress") || label.includes("во тек") || label.includes("tek")) return "in-progress";
        if (label.includes("schedul") || label.includes("закаж") || label.includes("zakaz")) return "scheduled";
        return "other";
    }
    function filteredRows() {
        const term = search.value.trim().toLocaleLowerCase();
        return appointments.filter(item => [item.patientFirstName, item.patientLastName, item.patientEmbg,
            item.doctorFirstName, item.doctorLastName, item.specialtyName]
            .some(value => String(value || "").toLocaleLowerCase().includes(term)));
    }
    function renderStatistics(rows) {
        document.getElementById("stat-total").textContent = rows.length;
        document.getElementById("stat-scheduled").textContent = rows.filter(x => statusGroup(x) === "scheduled").length;
        document.getElementById("stat-completed").textContent = rows.filter(x => statusGroup(x) === "completed").length;
        document.getElementById("stat-in-progress").textContent = rows.filter(x => statusGroup(x) === "in-progress").length;
    }
    function cell(row, value) {
        const td = row.insertCell();
        td.textContent = value == null ? "" : String(value);
        return td;
    }
    function render() {
        const rows = filteredRows();
        rows.sort((a, b) => {
            let left, right;
            if (sortKey === "date") {
                left = new Date(a.appointmentTime).getTime();
                right = new Date(b.appointmentTime).getTime();
            } else if (sortKey === "time") {
                const leftDate = new Date(a.appointmentTime);
                const rightDate = new Date(b.appointmentTime);
                left = leftDate.getHours() * 60 + leftDate.getMinutes();
                right = rightDate.getHours() * 60 + rightDate.getMinutes();
            } else if (sortKey === "patient") {
                left = `${a.patientFirstName || ""} ${a.patientLastName || ""}`.toLocaleLowerCase();
                right = `${b.patientFirstName || ""} ${b.patientLastName || ""}`.toLocaleLowerCase();
            } else if (sortKey === "doctor") {
                left = `${a.doctorFirstName || ""} ${a.doctorLastName || ""}`.toLocaleLowerCase();
                right = `${b.doctorFirstName || ""} ${b.doctorLastName || ""}`.toLocaleLowerCase();
            } else {
                left = String(a.status_name || "").toLocaleLowerCase();
                right = String(b.status_name || "").toLocaleLowerCase();
            }
            return (left < right ? -1 : left > right ? 1 : 0) * sortDirection;
        });
        renderStatistics(rows);
        const pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
        currentPage = Math.min(currentPage, pageCount);
        const start = (currentPage - 1) * pageSize;
        body.replaceChildren();
        const visible = rows.slice(start, start + pageSize);
        if (visible.length === 0) {
            const tr = body.insertRow();
            const td = tr.insertCell(); td.colSpan = 8; td.className = "text-center"; td.textContent = "No appointments found.";
        }
        for (const item of visible) {
            const tr = body.insertRow();
            const date = new Date(item.appointmentTime);
            cell(tr, Number.isNaN(date.getTime()) ? "" : date.toLocaleDateString());
            cell(tr, Number.isNaN(date.getTime()) ? "" : date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }));
            cell(tr, `${item.patientFirstName || ""} ${item.patientLastName || ""}`.trim());
            cell(tr, item.patientEmbg);
            cell(tr, `${item.doctorFirstName || ""} ${item.doctorLastName || ""}`.trim());
            cell(tr, item.specialtyName);
            cell(tr, item.status_name);
            cell(tr, item.notes);
        }
        pageLabel.textContent = `Page ${currentPage} of ${pageCount}`;
        previous.disabled = currentPage <= 1;
        next.disabled = currentPage >= pageCount;
    }
    async function loadAppointments() {
        error.classList.add("d-none");
        const dateFrom = form.elements.namedItem("dateFrom").value;
        const dateTo = form.elements.namedItem("dateTo").value;
        if (dateFrom && dateTo && dateFrom > dateTo) {
            error.textContent = "Date from must be on or before Date to.";
            error.classList.remove("d-none");
            return;
        }

        if (activeRequest) activeRequest.abort();
        activeRequest = new AbortController();
        const params = new URLSearchParams(new FormData(form));
        for (const [key, value] of [...params.entries()]) if (!value) params.delete(key);
        try {
            const response = await fetch(`${page.dataset.dataUrl}?${params.toString()}`, {
                headers: { Accept: "application/json" },
                signal: activeRequest.signal
            });
            if (!response.ok) throw new Error("Could not load appointments. Please try again.");
            appointments = await response.json();
            currentPage = 1;
            render();
        } catch (e) {
            if (e.name === "AbortError") return;
            error.textContent = e.message;
            error.classList.remove("d-none");
            appointments = [];
            render();
        }
    }
    form.addEventListener("submit", event => { event.preventDefault(); loadAppointments(); });
    form.addEventListener("reset", () => setTimeout(() => {
        error.classList.add("d-none");
        loadAppointments();
    }));
    search.addEventListener("input", () => { currentPage = 1; render(); });
    document.querySelectorAll(".sort-button").forEach(button => button.addEventListener("click", () => {
        if (sortKey === button.dataset.sort) sortDirection *= -1;
        else { sortKey = button.dataset.sort; sortDirection = 1; }
        render();
    }));
    previous.addEventListener("click", () => { if (currentPage > 1) { currentPage--; render(); } });
    next.addEventListener("click", () => { currentPage++; render(); });
    loadAppointments();
});
