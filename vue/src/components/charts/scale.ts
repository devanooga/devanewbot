export interface Scale {
    max: number;
    ticks: number[];
}

// Rounds the axis up to 1, 2, 2.5 or 5 times a power of ten so tick labels stay clean.
export function niceScale(maxValue: number, tickCount = 4): Scale {
    if (maxValue <= 0) {
        return { max: 1, ticks: [0, 1] };
    }

    const rough = maxValue / tickCount;
    const magnitude = Math.pow(10, Math.floor(Math.log10(rough)));
    const step = [1, 2, 2.5, 5, 10].map((m) => m * magnitude).find((candidate) => candidate >= rough) ?? rough;
    const max = Math.ceil(maxValue / step) * step;
    const ticks: number[] = [];
    for (let value = 0; value <= max + step / 2; value += step) {
        ticks.push(Math.round(value * 100) / 100);
    }
    return { max, ticks };
}

const whole = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 });
const exact = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });

export function dollars(value: number): string {
    return whole.format(value);
}

export function money(value: number): string {
    return exact.format(value);
}

// A column with a 4px rounded data end and a square foot on the baseline.
export function columnPath(x: number, y: number, width: number, height: number): string {
    if (height <= 0) {
        return "";
    }
    const r = Math.min(4, width / 2, height);
    return `M${x},${y + height}V${y + r}Q${x},${y} ${x + r},${y}H${x + width - r}Q${x + width},${y} ${x + width},${y + r}V${y + height}Z`;
}

// A bar growing right from the axis, rounded at its tip only.
export function barPath(x: number, y: number, width: number, height: number): string {
    if (width <= 0) {
        return "";
    }
    const r = Math.min(4, height / 2, width);
    return `M${x},${y}H${x + width - r}Q${x + width},${y} ${x + width},${y + r}V${y + height - r}Q${x + width},${y + height} ${x + width - r},${y + height}H${x}Z`;
}
