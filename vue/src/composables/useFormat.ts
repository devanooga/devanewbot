export function when(value: string | null | undefined): string {
    return value ? new Date(value).toLocaleString() : "";
}

export function timeOnly(value: string | null | undefined): string {
    return value ? new Date(value).toLocaleTimeString() : "";
}

export function ago(value: string | null | undefined): string {
    if (!value) {
        return "never";
    }

    const seconds = (Date.now() - new Date(value).getTime()) / 1000;
    if (seconds < 60) {
        return "just now";
    }
    if (seconds < 3600) {
        return `${Math.floor(seconds / 60)}m ago`;
    }
    if (seconds < 86400) {
        return `${Math.floor(seconds / 3600)}h ago`;
    }
    return `${Math.floor(seconds / 86400)}d ago`;
}

export function kilometres(value: number | null | undefined): string {
    return value == null ? "" : `${Math.round(value).toLocaleString()} km`;
}

export function megahertz(hz: number): string {
    return `${(hz / 1_000_000).toFixed(3)} MHz`;
}

export function count(value: number): string {
    return value.toLocaleString();
}
