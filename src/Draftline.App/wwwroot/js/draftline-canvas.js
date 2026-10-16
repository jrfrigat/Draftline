// The canvas bridge: it reports what the pointer and the keyboard did, and decides nothing.
//
// Everything that needs a decision - what was under the pointer, which handle was grabbed, where a
// block ends up - happens in C#, where the document is. What only the browser knows stays here: the
// coordinates of an event, pointer capture while a button is held, and the fact that a drop is a
// browser gesture at all.

let canvas = null;
let dotNet = null;
let activePointer = null;
let hand = false, pan = null;
let viewportObserver = null;
let hasSelection = false, canPaste = false;
export function setSelectionState(selected, paste) { hasSelection = selected; canPaste = paste; }
export function isCoarsePointer() { return window.matchMedia('(pointer: coarse)').matches; }
export function setHand(value) { hand = value; }
export function availableWidth() { return Math.max(100, canvas?.parentElement.clientWidth - 28); }
function onWheel(event) {
    if (!event.ctrlKey && !event.metaKey) return;
    event.preventDefault();
    dotNet?.invokeMethodAsync('ZoomBy', event.deltaY < 0 ? .1 : -.1);
}

// Initial editor layout only; the person's panel choices take over after startup.
export function isCompactViewport() {
    return window.matchMedia('(max-width: 900px)').matches;
}

export function scrollToElement(id) {
    if (!canvas) return;
    const element = Array.from(canvas.querySelectorAll('[data-element-id]')).find(e => e.dataset.elementId === id);
    element?.scrollIntoView({block:'center', inline:'center'});
}

// Attaches to a sheet. Called once, when the canvas first renders.
export function attach(element, reference) {
    detach();

    canvas = element;
    dotNet = reference;
    viewportObserver = new ResizeObserver(() => dotNet?.invokeMethodAsync('OnViewportChanged', availableWidth()));
    viewportObserver.observe(canvas.parentElement);

    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerup', onPointerUp);
    canvas.addEventListener('pointercancel', onPointerUp);
    canvas.addEventListener('dragover', onDragOver);
    canvas.addEventListener('drop', onDrop);
    canvas.addEventListener('dblclick', onDoubleClick);
    canvas.addEventListener('wheel', onWheel, {passive:false});
    document.addEventListener('keydown', onKeyDown);
}

// Takes everything away again, so navigating between pages does not leave listeners behind that
// point at a component the app has already thrown out.
export function detach() {
    viewportObserver?.disconnect(); viewportObserver = null;
    if (canvas) {
        canvas.removeEventListener('pointerdown', onPointerDown);
        canvas.removeEventListener('pointermove', onPointerMove);
        canvas.removeEventListener('pointerup', onPointerUp);
        canvas.removeEventListener('pointercancel', onPointerUp);
        canvas.removeEventListener('dragover', onDragOver);
        canvas.removeEventListener('drop', onDrop);
        canvas.removeEventListener('dblclick', onDoubleClick);
        canvas.removeEventListener('wheel', onWheel);
    }

    document.removeEventListener('keydown', onKeyDown);

    canvas = null;
    dotNet = null;
    activePointer = null;
    hand = false; pan = null;
    hasSelection = canPaste = false;
}

// Where the pointer is, in the sheet's own pixels. The difference between the two rectangles is the
// same in both coordinate systems, which is what makes this right while the page is scrolled and the
// sheet sits inside a scrolling host.
function pointOf(event) {
    const rect = canvas.getBoundingClientRect();
    return { x: (event.clientX - rect.left) * canvas.offsetWidth / rect.width,
        y: (event.clientY - rect.top) * canvas.offsetHeight / rect.height };
}

function onPointerDown(event) {
    if (!canvas || !dotNet) {
        return;
    }

    if (event.pointerType === 'mouse' && event.button !== 0) {
        return;
    }
    if (event.target.closest('input, textarea, button, .df-inline-editor')) return;
    if (hand) {
        if (event.pointerType === 'touch') return; // Native scrolling and pinch navigation in hand mode.
        const host = canvas.parentElement;
        pan = {x:event.clientX,y:event.clientY,left:host.scrollLeft,top:host.scrollTop};
        canvas.setPointerCapture?.(event.pointerId); activePointer = event.pointerId;
        event.preventDefault(); return;
    }

    // Capture keeps the events coming while the pointer is outside the sheet - dragging a block off
    // the edge is exactly the moment a drag used to be lost.
    if (canvas.setPointerCapture) {
        canvas.setPointerCapture(event.pointerId);
    }

    activePointer = event.pointerId;
    event.preventDefault();

    const point = pointOf(event);
    dotNet.invokeMethodAsync('OnPointerDown', point.x, point.y, event.ctrlKey || event.metaKey);
}

function onDoubleClick(event) {
    if (!canvas || !dotNet || hand || event.target.closest('input, textarea, .df-inline-editor')) return;
    const point = pointOf(event);
    dotNet.invokeMethodAsync('OnEditText', point.x, point.y);
}

function onPointerMove(event) {
    if (!canvas || !dotNet) {
        return;
    }
    if (hand) {
        if (pan && activePointer === event.pointerId) {
            canvas.parentElement.scrollLeft = pan.left - (event.clientX - pan.x);
            canvas.parentElement.scrollTop = pan.top - (event.clientY - pan.y);
        }
        return;
    }

    if (activePointer !== null && event.pointerId !== activePointer) {
        return;
    }

    // Hovering is not a gesture: the canvas only hears about a pointer that is holding a button.
    if (activePointer === null && event.buttons === 0) {
        return;
    }

    if (activePointer !== null) {
        event.preventDefault();
    }

    const point = pointOf(event);
    dotNet.invokeMethodAsync('OnPointerMove', point.x, point.y, event.ctrlKey || event.metaKey);
}

function onPointerUp(event) {
    if (!canvas || !dotNet) {
        return;
    }

    if (activePointer !== null && event.pointerId !== activePointer) {
        return;
    }

    if (canvas.hasPointerCapture && canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
    }

    activePointer = null;
    if (hand) { pan = null; return; }

    const point = pointOf(event);
    dotNet.invokeMethodAsync('OnPointerUp', point.x, point.y);
}

// The palette's drag ends here. What was being carried is not read out of the event: the app already
// knows (PaletteDrag), so a browser that restricts drag data changes nothing about dropping.
function onDragOver(event) {
    event.preventDefault();

    if (event.dataTransfer) {
        event.dataTransfer.dropEffect = 'copy';
    }
}

function onDrop(event) {
    if (!canvas || !dotNet) {
        return;
    }

    event.preventDefault();

    const point = pointOf(event);
    dotNet.invokeMethodAsync('OnDrop', point.x, point.y);
}

// The keyboard belongs to the editor only while the person is not typing into a field: an inspector
// where Backspace deletes a block instead of a character is a trap.
function isTyping(target) {
    if (!target) {
        return false;
    }

    const tag = target.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target.isContentEditable === true;
}

function onKeyDown(event) {
    if (dotNet && event.key === 'Escape' && event.target.closest('.df-document-search')) {
        event.preventDefault();
        return dotNet.invokeMethodAsync('OnKey', 'CloseSearch', false);
    }
    if (dotNet && (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'f') {
        event.preventDefault();
        return dotNet.invokeMethodAsync('OnKey', 'Search', false);
    }
    if (!dotNet || isTyping(event.target)) {
        return;
    }
    if ((event.ctrlKey || event.metaKey) && event.key.startsWith('Arrow') && canvas?.contains(event.target)) {
        event.preventDefault();
        return dotNet.invokeMethodAsync('SelectAdjacent', event.key === 'ArrowLeft' || event.key === 'ArrowUp');
    }
    if (event.target.closest('[role="tab"]')) {
        if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) event.preventDefault();
        return;
    }

    const ctrl = event.ctrlKey || event.metaKey;
    let key = null;

    if (ctrl && (event.key === 'g' || event.key === 'G')) {
        key = event.shiftKey ? 'Ungroup' : 'Group';
    } else if (event.key === 'Delete' || event.key === 'Backspace') {
        key = 'Delete';
    } else if (event.key === 'F2') {
        key = 'EditText';
    } else if (event.key === 'Escape') {
        key = 'Escape';
    } else if (ctrl && (event.key === 'c' || event.key === 'C')) {
        key = 'Copy';
    } else if (ctrl && (event.key === 'v' || event.key === 'V')) {
        key = 'Paste';
    } else if (ctrl && (event.key === 'x' || event.key === 'X')) {
        key = 'Cut';
    } else if (ctrl && (event.key === 'd' || event.key === 'D')) {
        key = 'Duplicate';
    } else if (ctrl && !event.shiftKey && (event.key === 'z' || event.key === 'Z')) {
        key = 'Undo';
    } else if (ctrl && (event.key === 'y' || event.key === 'Y')) {
        key = 'Redo';
    } else if (ctrl && event.shiftKey && (event.key === 'z' || event.key === 'Z')) {
        key = 'Redo';
    } else if (event.key === 'ArrowLeft') {
        key = 'Left';
    } else if (event.key === 'ArrowRight') {
        key = 'Right';
    } else if (event.key === 'ArrowUp') {
        key = 'Up';
    } else if (event.key === 'ArrowDown') {
        key = 'Down';
    }

    if (!key) {
        return;
    }
    if (['Copy','Cut','Delete','Duplicate','Group','Ungroup','Front','Back'].includes(key) && !hasSelection) return;
    if (key === 'Paste' && !canPaste) return;
    if (key === 'Copy' && window.getSelection()?.toString()) return;

    event.preventDefault();
    return dotNet.invokeMethodAsync('OnKey', key, event.shiftKey);
}
