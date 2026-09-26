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

const MilesPerKilometre = 0.621371;

export function kilometres(value: number | null | undefined): string {
    if (value == null) {
        return "";
    }

    const miles = Math.round(value * MilesPerKilometre).toLocaleString();
    return `${Math.round(value).toLocaleString()} km / ${miles} mi`;
}

export function megahertz(hz: number): string {
    return `${(hz / 1_000_000).toFixed(3)} MHz`;
}

export function count(value: number): string {
    return value.toLocaleString();
}

export interface InlineSegment {
    kind: "text" | "code" | "link";
    text: string;
    href?: string;
}

// The imported history was written as GitHub markdown, so it carries `code` spans and [links](url).
export function inlineSegments(value: string): InlineSegment[] {
    const segments: InlineSegment[] = [];
    const pattern = /`([^`]+)`|\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g;
    let last = 0;
    for (const match of value.matchAll(pattern)) {
        const index = match.index ?? 0;
        if (index > last) {
            segments.push({ kind: "text", text: value.slice(last, index) });
        }
        segments.push(
            match[1] !== undefined
                ? { kind: "code", text: match[1] }
                : { kind: "link", text: match[2], href: match[3] },
        );
        last = index + match[0].length;
    }
    if (last < value.length) {
        segments.push({ kind: "text", text: value.slice(last) });
    }
    return segments;
}
