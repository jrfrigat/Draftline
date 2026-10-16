// Run before Flare's bootstrap: old editor themes must not paint a Material shell.
try {
    localStorage.setItem('flare-theme', 'draftline-fluent2');
    localStorage.setItem('flare-theme-lineage', 'draftline-fluent2 fluent2');
} catch {
    // Flare's default still selects Fluent when persistence is unavailable.
}
