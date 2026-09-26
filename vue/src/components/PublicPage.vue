<template>
    <div class="public-page">
        <div class="public-content">
            <header>
                <h1>{{ title }}</h1>
                <p><slot name="intro" /></p>
            </header>

            <p v-if="failed" class="status">Could not load this page. Try again in a moment.</p>
            <p v-else-if="loading" class="status">Loading…</p>
            <slot v-else />
        </div>
    </div>
</template>

<script setup lang="ts">
defineProps<{ title: string; loading: boolean; failed: boolean }>();
</script>

<style>
body {
    margin: 0;
}

.public-page {
    --page: #f5f6f8;
    --surface: #ffffff;
    --text: #1d2026;
    --muted: #5b6472;
    --border: #e1e4e8;
    --code: #eef0f3;
    --link: #c4513d;
    --positive: #1a7f37;
    --accent: #c4513d;
    --series-in: #2a78d6;
    --series-out: #eb6834;
    --grid: #e7e9ec;
    --axis: #c9ced6;
    min-height: 100vh;
    background: var(--page);
    color: var(--text);
}

@media (prefers-color-scheme: dark) {
    .public-page {
        --page: #0f1115;
        --surface: #181b22;
        --text: #e6e8eb;
        --muted: #9aa3b0;
        --border: #2a2e37;
        --code: #242833;
        --link: #f08a77;
        --positive: #3fb950;
        --series-in: #3987e5;
        --series-out: #d95926;
        --grid: #252a33;
        --axis: #3a404b;
    }
}

.public-content {
    box-sizing: border-box;
    max-width: 1100px;
    margin: 0 auto;
    padding: 48px 16px;
    font-family: "Open Sans", sans-serif;
    line-height: 1.5;
}

.public-page header {
    margin-bottom: 24px;
}

.public-page h1 {
    font-size: 32px;
    font-weight: 700;
    margin: 0 0 8px;
}

.public-page header p,
.public-page .status {
    color: var(--muted);
    margin: 0;
}

.public-page a {
    color: var(--link);
}

.public-page .muted {
    color: var(--muted);
}

.public-page .card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 8px;
    overflow: hidden;
}

.public-page table {
    width: 100%;
    border-collapse: collapse;
    font-size: 15px;
}

.public-page th {
    text-align: left;
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--muted);
    padding: 12px 16px;
    border-bottom: 1px solid var(--border);
}

.public-page td {
    vertical-align: top;
    padding: 12px 16px;
    border-bottom: 1px solid var(--border);
    overflow-wrap: anywhere;
}

.public-page tbody tr:last-child td {
    border-bottom: 0;
}

.public-page code {
    font-size: 0.9em;
    background: var(--code);
    border-radius: 4px;
    padding: 1px 4px;
}

@media (max-width: 720px) {
    .public-content {
        padding: 32px 16px;
    }

    .public-page thead {
        display: none;
    }

    .public-page table,
    .public-page tbody,
    .public-page tr,
    .public-page td {
        display: block;
    }

    .public-page tr {
        padding: 12px 16px;
        border-bottom: 1px solid var(--border);
    }

    .public-page tbody tr:last-child {
        border-bottom: 0;
    }

    .public-page td {
        padding: 2px 0;
        border: 0;
    }
}
</style>
