// ==================== Character Customization ====================

var settingsVersion = 0; // Track which settings response is latest

function changePreviewModel(hash) {
    settingsVersion++;
    console.log('[shop] changePreviewModel:', hash);
    fetch('https://gamemodecity/changeModel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modelHash: hash })
    }).then(nuiResp).then(function(data) {
        console.log('[shop] changeModel response:', JSON.stringify(data));
        if (data.status === 'ok') {
            customizeSettings = data.settings;
            currentAppearance = data.appearance;
            console.log('[shop] isFreemode:', customizeSettings && customizeSettings.isFreemode);
            updateSubTabStates();
            renderCurrentSection();
        }
    }).catch(function(err) {
        console.log('[shop] changeModel ERROR:', err);
    });
}

function enterCustomization() {
    previewingModelHash = null;
    purchaseConfirmHash = null;

    // Immediately show the model section (it only needs pedModels + progression)
    customizeActive = true;
    currentSection = 'model';
    renderCurrentSection();
    updateSubTabStates(); // Disable non-model tabs until ped ready

    // Spawn preview ped in background — update settings when ready
    settingsVersion++;
    var myVersion = settingsVersion;
    console.log('[shop] enterCustomization: firing customizeStart');
    fetch('https://gamemodecity/customizeStart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    }).then(nuiResp).then(function(data) {
        console.log('[shop] customizeStart response:', JSON.stringify(data), 'myVersion:', myVersion, 'settingsVersion:', settingsVersion);
        if (data.status !== 'ok') {
            if (data.reason === 'in_game') {
                customizeActive = false;
                document.getElementById('hub').classList.remove('shop-preview-active');
                switchTab('home');
                showAdminStatus('Cannot customize during an active game', false);
            }
            return;
        }
        // Only apply if user hasn't changed model since we started
        if (settingsVersion !== myVersion) return;
        customizeSettings = data.settings;
        currentAppearance = data.appearance;
        console.log('[shop] customizeStart: isFreemode:', customizeSettings && customizeSettings.isFreemode);
        updateSubTabStates();
        renderCurrentSection();
    }).catch(function(err) {
        console.log('[shop] customizeStart ERROR:', err);
    });
}

function exitCustomization(cancelled) {
    if (!customizeActive) return;
    customizeActive = false;
    previewingModelHash = null;
    purchaseConfirmHash = null;
    var endpoint = cancelled ? 'customizeCancel' : 'customizeSave';
    fetch('https://gamemodecity/' + endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
    customizeSettings = null;
    currentAppearance = null;
}

function updateSubTabStates() {
    var isFreemode = customizeSettings && customizeSettings.isFreemode;
    var pedReady = customizeSettings != null;
    document.querySelectorAll('.shop-sub-tab').forEach(function(btn) {
        var section = btn.getAttribute('data-section');
        if (section === 'model') {
            btn.classList.remove('disabled');
        } else if (!pedReady) {
            // Disable all non-model tabs until preview ped is ready
            btn.classList.add('disabled');
        } else if ((section === 'face' || section === 'overlays' || section === 'eyes') && !isFreemode) {
            btn.classList.add('disabled');
        } else {
            btn.classList.remove('disabled');
        }
    });
}

function setCamera(preset) {
    fetch('https://gamemodecity/setCameraPreset', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ preset: preset })
    });
    document.querySelectorAll('.cam-btn').forEach(function(b) { b.classList.remove('active'); });
    var active = document.querySelector('.cam-btn[data-preset="' + preset + '"]');
    if (active) active.classList.add('active');
}

function autoCameraForSection(section) {
    if (section === 'face' || section === 'hair' || section === 'eyes' || section === 'overlays') {
        setCamera('head');
    } else if (section === 'clothing' || section === 'accessories') {
        setCamera('body');
    } else {
        setCamera('default');
    }
}

function renderCurrentSection() {
    var container = document.getElementById('shopSectionContent');
    container.innerHTML = '';
    switch (currentSection) {
        case 'model': renderModelSection(container); break;
        case 'face': renderFaceSection(container); break;
        case 'hair': renderHairSection(container); break;
        case 'overlays': renderOverlaysSection(container); break;
        case 'clothing': renderClothingSection(container); break;
        case 'accessories': renderAccessoriesSection(container); break;
        case 'eyes': renderEyesSection(container); break;
    }
}

// ---- Model Section ----
var previewingModelHash = null;
var purchaseConfirmHash = null;

function getModelCost(category) {
    if (category === 'Freemode') return 100000;
    if (category === 'Special') return 1000;
    return 500;
}

function formatTokens(amount) {
    return amount.toLocaleString() + 'T';
}

function renderModelSection(container) {
    // Category filters
    var filters = document.createElement('div');
    filters.className = 'shop-filters';
    ['All','Freemode','Male','Female','Special'].forEach(function(cat) {
        var btn = document.createElement('button');
        btn.className = 'shop-filter-btn';
        if (shopCategory === cat) btn.classList.add('active');
        btn.textContent = cat;
        btn.addEventListener('click', function() {
            shopCategory = cat;
            purchaseConfirmHash = null;
            renderCurrentSection();
        });
        filters.appendChild(btn);
    });
    container.appendChild(filters);

    // Grid
    var grid = document.createElement('div');
    grid.className = 'shop-grid';

    pedModels.forEach(function(model) {
        if (shopCategory !== 'All' && model.category !== shopCategory) return;

        var isOwned = progression.unlockedModels.indexOf(model.hash) !== -1;
        var isSelected = progression.selectedModel === model.hash;
        var isPreviewing = previewingModelHash === model.hash;
        var modelCost = getModelCost(model.category);
        var canAfford = progression.tokens >= modelCost;

        var card = document.createElement('div');
        card.className = 'shop-card';
        if (isPreviewing) card.classList.add('previewing');
        if (isSelected) card.classList.add('selected');
        else if (isOwned) card.classList.add('owned');
        else card.classList.add('locked');

        var icon = document.createElement('div');
        icon.className = 'shop-card-icon';
        icon.textContent = model.category === 'Male' ? 'M' : model.category === 'Female' ? 'F' : 'S';
        card.appendChild(icon);

        var name = document.createElement('div');
        name.className = 'shop-card-name';
        name.textContent = model.name;
        card.appendChild(name);

        var cat = document.createElement('div');
        cat.className = 'shop-card-category';
        cat.textContent = model.category;
        card.appendChild(cat);

        // Click card to preview ANY model (owned or not)
        (function(hash) {
            card.addEventListener('click', function() {
                previewingModelHash = hash;
                purchaseConfirmHash = null;
                changePreviewModel(hash);
            });
        })(model.hash);

        // Action button
        var btn = document.createElement('button');
        btn.className = 'shop-card-btn';
        if (isSelected) {
            btn.classList.add('selected-btn');
            btn.textContent = 'Selected';
        } else if (isOwned) {
            btn.classList.add('select-btn');
            btn.textContent = 'Select';
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    fetch('https://gamemodecity/selectModel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ modelHash: hash })
                    });
                    // Also update preview ped to this model
                    previewingModelHash = hash;
                    changePreviewModel(hash);
                });
            })(model.hash);
        } else if (purchaseConfirmHash === model.hash) {
            // Confirm purchase state
            btn.classList.add('confirm-btn');
            btn.textContent = 'Confirm - ' + formatTokens(modelCost);
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    btn.textContent = 'Buying...';
                    btn.disabled = true;
                    purchaseConfirmHash = null;
                    fetch('https://gamemodecity/purchaseModel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ modelHash: hash })
                    });
                });
            })(model.hash);
        } else if (canAfford) {
            btn.classList.add('buy-btn');
            btn.textContent = formatTokens(modelCost);
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    purchaseConfirmHash = hash;
                    renderCurrentSection();
                });
            })(model.hash);
        } else {
            btn.classList.add('cant-afford');
            btn.textContent = formatTokens(modelCost);
        }
        card.appendChild(btn);
        grid.appendChild(card);
    });
    container.appendChild(grid);
}

// ---- Face Section ----
function renderFaceSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Face customization is only available for Freemode models</div>';
        return;
    }

    // Head Blend
    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Genetics';
    container.appendChild(title);

    var blendRow = document.createElement('div');
    blendRow.className = 'cust-head-blend-row';

    var hb = (currentAppearance && currentAppearance.headBlend) || {};
    var fields = [
        { label: 'Mother', key: 'shapeFirst', max: 45 },
        { label: 'Father', key: 'shapeSecond', max: 45 },
        { label: 'Shape Mix', key: 'shapeMix', isFloat: true },
        { label: 'Skin Mother', key: 'skinFirst', max: 45 },
        { label: 'Skin Father', key: 'skinSecond', max: 45 },
        { label: 'Skin Mix', key: 'skinMix', isFloat: true }
    ];

    fields.forEach(function(f) {
        var col = document.createElement('div');
        col.className = 'cust-head-blend-col';
        var lbl = document.createElement('label');
        lbl.textContent = f.label;
        col.appendChild(lbl);

        if (f.isFloat) {
            var inp = document.createElement('input');
            inp.type = 'range';
            inp.className = 'cust-slider';
            inp.min = '0'; inp.max = '1'; inp.step = '0.05';
            inp.value = hb[f.key] || 0.5;
            inp.addEventListener('input', function() {
                hb[f.key] = parseFloat(inp.value);
                sendHeadBlend(hb);
            });
            col.appendChild(inp);
        } else {
            var inp = document.createElement('input');
            inp.type = 'number';
            inp.min = '0'; inp.max = String(f.max);
            inp.value = hb[f.key] || 0;
            inp.addEventListener('change', function() {
                hb[f.key] = parseInt(inp.value) || 0;
                sendHeadBlend(hb);
            });
            col.appendChild(inp);
        }
        blendRow.appendChild(col);
    });
    container.appendChild(blendRow);

    // Face Features (sliders)
    var title2 = document.createElement('div');
    title2.className = 'cust-section-title';
    title2.textContent = 'Face Features';
    container.appendChild(title2);

    var ff2 = (currentAppearance && currentAppearance.faceFeatures) || [];
    for (var i = 0; i < 20; i++) {
        (function(idx) {
            var row = document.createElement('div');
            row.className = 'cust-row';

            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = faceFeatureNames[idx] || ('Feature ' + idx);
            row.appendChild(lbl);

            var slider = document.createElement('input');
            slider.type = 'range';
            slider.className = 'cust-slider';
            slider.min = '-1'; slider.max = '1'; slider.step = '0.05';
            slider.value = ff2[idx] || 0;

            var valSpan = document.createElement('span');
            valSpan.className = 'cust-slider-value';
            valSpan.textContent = parseFloat(slider.value).toFixed(2);

            slider.addEventListener('input', function() {
                valSpan.textContent = parseFloat(slider.value).toFixed(2);
                if (!ff2[idx] && ff2.length <= idx) {
                    while (ff2.length <= idx) ff2.push(0);
                }
                ff2[idx] = parseFloat(slider.value);
                fetch('https://gamemodecity/changeFaceFeature', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ index: idx, value: parseFloat(slider.value) })
                });
            });

            row.appendChild(slider);
            row.appendChild(valSpan);
            container.appendChild(row);
        })(i);
    }
}

function sendHeadBlend(hb) {
    fetch('https://gamemodecity/changeHeadBlend', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            shapeFirst: hb.shapeFirst || 0,
            shapeSecond: hb.shapeSecond || 0,
            shapeMix: hb.shapeMix || 0.5,
            skinFirst: hb.skinFirst || 0,
            skinSecond: hb.skinSecond || 0,
            skinMix: hb.skinMix || 0.5
        })
    });
}

// ---- Hair Section ----
function renderHairSection(container) {
    var hair = (currentAppearance && currentAppearance.hair) || { style: 0, color: 0, highlight: 0 };
    var maxHair = customizeSettings ? customizeSettings.components[2].maxDrawable : 1;

    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Hair Style';
    container.appendChild(title);

    // Style prev/next
    var row = document.createElement('div');
    row.className = 'cust-row';

    var lbl = document.createElement('span');
    lbl.className = 'cust-row-label';
    lbl.textContent = 'Style';
    row.appendChild(lbl);

    var pn = createPrevNext(hair.style, maxHair, function(val) {
        hair.style = val;
        sendHair(hair);
        checkHairUnlock(val);
    });
    row.appendChild(pn.el);

    // Unlock badge
    var badge = document.createElement('span');
    badge.className = 'cust-buy-badge';
    badge.id = 'hairUnlockBadge';
    updateHairBadge(badge, hair.style);
    row.appendChild(badge);
    container.appendChild(row);

    // Color
    var title2 = document.createElement('div');
    title2.className = 'cust-section-title';
    title2.textContent = 'Hair Color';
    container.appendChild(title2);

    var colorGrid = document.createElement('div');
    colorGrid.className = 'cust-color-grid';
    for (var c = 0; c < 64; c++) {
        (function(idx) {
            var swatch = document.createElement('div');
            swatch.className = 'cust-color-swatch';
            swatch.style.background = hairColors[idx] || '#555';
            if (idx === hair.color) swatch.classList.add('active');
            swatch.addEventListener('click', function() {
                hair.color = idx;
                sendHair(hair);
                colorGrid.querySelectorAll('.cust-color-swatch').forEach(function(s) { s.classList.remove('active'); });
                swatch.classList.add('active');
            });
            colorGrid.appendChild(swatch);
        })(c);
    }
    container.appendChild(colorGrid);

    // Highlight
    var title3 = document.createElement('div');
    title3.className = 'cust-section-title';
    title3.textContent = 'Highlight Color';
    container.appendChild(title3);

    var hlGrid = document.createElement('div');
    hlGrid.className = 'cust-color-grid';
    for (var h = 0; h < 64; h++) {
        (function(idx) {
            var swatch = document.createElement('div');
            swatch.className = 'cust-color-swatch';
            swatch.style.background = hairColors[idx] || '#555';
            if (idx === hair.highlight) swatch.classList.add('active');
            swatch.addEventListener('click', function() {
                hair.highlight = idx;
                sendHair(hair);
                hlGrid.querySelectorAll('.cust-color-swatch').forEach(function(s) { s.classList.remove('active'); });
                swatch.classList.add('active');
            });
            hlGrid.appendChild(swatch);
        })(h);
    }
    container.appendChild(hlGrid);
}

function sendHair(hair) {
    fetch('https://gamemodecity/changeHair', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ style: hair.style, color: hair.color, highlight: hair.highlight })
    });
}

function checkHairUnlock(style) {
    var badge = document.getElementById('hairUnlockBadge');
    if (badge) updateHairBadge(badge, style);
}

function updateHairBadge(badge, style) {
    badge.textContent = 'Free';
    badge.classList.add('owned');
}

// ---- Overlays Section ----
function renderOverlaysSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Features are only available for Freemode models</div>';
        return;
    }

    var overlays = (currentAppearance && currentAppearance.headOverlays) || [];
    var maxOverlays = customizeSettings.overlays || [];

    for (var i = 0; i < 12; i++) {
        (function(idx) {
            var maxVal = maxOverlays[idx] || 1;
            var ov = overlays[idx] || { index: 255, opacity: 1, colorType: 0, firstColor: 0, secondColor: 0 };

            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = overlayNames[idx];
            container.appendChild(title);

            // Style prev/next
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            // -1 = none (255), then 0 to maxVal-1
            var displayVal = ov.index === 255 ? -1 : ov.index;
            var pn = createPrevNext(displayVal, maxVal, function(val) {
                ov.index = val < 0 ? 255 : val;
                sendOverlay(idx, ov);
            }, -1);
            row.appendChild(pn.el);
            container.appendChild(row);

            // Opacity slider
            var opRow = document.createElement('div');
            opRow.className = 'cust-row';
            var opLbl = document.createElement('span');
            opLbl.className = 'cust-row-label';
            opLbl.textContent = 'Opacity';
            opRow.appendChild(opLbl);

            var opSlider = document.createElement('input');
            opSlider.type = 'range';
            opSlider.className = 'cust-slider';
            opSlider.min = '0'; opSlider.max = '1'; opSlider.step = '0.05';
            opSlider.value = ov.opacity;

            var opVal = document.createElement('span');
            opVal.className = 'cust-slider-value';
            opVal.textContent = parseFloat(opSlider.value).toFixed(2);

            opSlider.addEventListener('input', function() {
                opVal.textContent = parseFloat(opSlider.value).toFixed(2);
                ov.opacity = parseFloat(opSlider.value);
                sendOverlay(idx, ov);
            });

            opRow.appendChild(opSlider);
            opRow.appendChild(opVal);
            container.appendChild(opRow);

            // Color (for overlays that support it: 1=facial hair, 2=eyebrows, 8=lipstick, 5=blush, 10=chest hair)
            var colorOverlays = [1, 2, 5, 8, 10];
            if (colorOverlays.indexOf(idx) !== -1) {
                var colorRow = document.createElement('div');
                colorRow.className = 'cust-row';
                var colorLbl = document.createElement('span');
                colorLbl.className = 'cust-row-label';
                colorLbl.textContent = 'Color';
                colorRow.appendChild(colorLbl);

                var colorPn = createPrevNext(ov.firstColor, 64, function(val) {
                    ov.firstColor = val;
                    ov.colorType = (idx === 5 || idx === 8) ? 2 : 1;
                    sendOverlay(idx, ov);
                });
                colorRow.appendChild(colorPn.el);
                container.appendChild(colorRow);
            }

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(i);
    }
}

function sendOverlay(overlayId, ov) {
    fetch('https://gamemodecity/changeHeadOverlay', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            overlayId: overlayId,
            index: ov.index,
            opacity: ov.opacity,
            colorType: ov.colorType || 0,
            firstColor: ov.firstColor || 0,
            secondColor: ov.secondColor || 0
        })
    });
}

// ---- Clothing Section ----
function renderClothingSection(container) {
    if (!customizeSettings) return;
    var comps = customizeSettings.components;
    var curComps = (currentAppearance && currentAppearance.components) || [];
    var isFreemode = customizeSettings.isFreemode;

    // Skip face(0) and hair(2) for freemode, show all for non-freemode
    for (var i = 0; i < 12; i++) {
        if (isFreemode && (i === 0 || i === 2)) continue;
        if (comps[i].maxDrawable <= 1) continue; // Skip slots with only 1 option

        (function(compId) {
            var maxDraw = comps[compId].maxDrawable;
            var cur = curComps[compId] || { drawable: 0, texture: 0 };
            var maxTex = comps[compId].maxTexture;

            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = componentNames[compId];
            container.appendChild(title);

            // Drawable prev/next
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            var texRow, texPn;
            var pn = createPrevNext(cur.drawable, maxDraw, function(val) {
                cur.drawable = val;
                cur.texture = 0;
                fetch('https://gamemodecity/changeComponent', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ componentId: compId, drawable: val, texture: 0 })
                }).then(nuiResp).then(function(data) {
                    if (data.maxTexture !== undefined) {
                        maxTex = data.maxTexture;
                        if (texPn && texPn.updateMax) texPn.updateMax(maxTex);
                    }
                });
                updateCompBadge(compId, val);
            });
            row.appendChild(pn.el);

            // Unlock badge
            var badge = document.createElement('span');
            badge.className = 'cust-buy-badge';
            badge.id = 'compBadge_' + compId;
            updateCompBadgeEl(badge, compId, cur.drawable);
            row.appendChild(badge);
            container.appendChild(row);

            // Texture prev/next
            if (maxTex > 1) {
                texRow = document.createElement('div');
                texRow.className = 'cust-row';
                var texLbl = document.createElement('span');
                texLbl.className = 'cust-row-label';
                texLbl.textContent = 'Texture';
                texRow.appendChild(texLbl);

                texPn = createPrevNext(cur.texture, maxTex, function(val) {
                    cur.texture = val;
                    fetch('https://gamemodecity/changeComponent', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ componentId: compId, drawable: cur.drawable, texture: val })
                    });
                });
                texRow.appendChild(texPn.el);
                container.appendChild(texRow);
            }

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(i);
    }
}

function updateCompBadge(compId, drawable) {
    var badge = document.getElementById('compBadge_' + compId);
    if (badge) updateCompBadgeEl(badge, compId, drawable);
}

function updateCompBadgeEl(badge, compId, drawable) {
    badge.textContent = 'Free';
    badge.classList.add('owned');
}

// ---- Accessories Section ----
function renderAccessoriesSection(container) {
    if (!customizeSettings) return;
    var propsSettings = customizeSettings.props;
    var curProps = (currentAppearance && currentAppearance.props) || [];

    propsSettings.forEach(function(ps, idx) {
        var propId = ps.id;
        var maxDraw = ps.maxDrawable;
        if (maxDraw <= 0) return;

        // Find current prop value
        var curProp = null;
        for (var j = 0; j < curProps.length; j++) {
            if (curProps[j].id === propId) { curProp = curProps[j]; break; }
        }
        if (!curProp) curProp = { id: propId, drawable: -1, texture: 0 };

        (function(pId, cur, maxD) {
            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = propNames[pId] || ('Prop ' + pId);
            container.appendChild(title);

            var maxTex = ps.maxTexture;

            // Drawable prev/next (-1 = none)
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            var texPn;
            var pn = createPrevNext(cur.drawable, maxD, function(val) {
                cur.drawable = val;
                cur.texture = 0;
                fetch('https://gamemodecity/changeProp', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ propId: pId, drawable: val, texture: 0 })
                }).then(nuiResp).then(function(data) {
                    if (data.maxTexture !== undefined) {
                        maxTex = data.maxTexture;
                        if (texPn && texPn.updateMax) texPn.updateMax(maxTex);
                    }
                });
                updatePropBadge(pId, val);
            }, -1);
            row.appendChild(pn.el);

            // Unlock badge
            var badge = document.createElement('span');
            badge.className = 'cust-buy-badge';
            badge.id = 'propBadge_' + pId;
            updatePropBadgeEl(badge, pId, cur.drawable);
            row.appendChild(badge);
            container.appendChild(row);

            // Texture prev/next
            var texRow = document.createElement('div');
            texRow.className = 'cust-row';
            var texLbl = document.createElement('span');
            texLbl.className = 'cust-row-label';
            texLbl.textContent = 'Texture';
            texRow.appendChild(texLbl);

            texPn = createPrevNext(cur.texture, Math.max(maxTex, 1), function(val) {
                cur.texture = val;
                fetch('https://gamemodecity/changeProp', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ propId: pId, drawable: cur.drawable, texture: val })
                });
            });
            texRow.appendChild(texPn.el);
            container.appendChild(texRow);

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(propId, curProp, maxDraw);
    });
}

function updatePropBadge(propId, drawable) {
    var badge = document.getElementById('propBadge_' + propId);
    if (badge) updatePropBadgeEl(badge, propId, drawable);
}

function updatePropBadgeEl(badge, propId, drawable) {
    badge.textContent = 'Free';
    badge.classList.add('owned');
}

// ---- Eyes Section ----
function renderEyesSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Eye color is only available for Freemode models</div>';
        return;
    }

    var curEye = (currentAppearance && currentAppearance.eyeColor) || 0;

    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Eye Color';
    container.appendChild(title);

    var grid = document.createElement('div');
    grid.className = 'cust-eye-grid';

    for (var i = 0; i < eyeColorNames.length; i++) {
        (function(idx) {
            var btn = document.createElement('button');
            btn.className = 'cust-eye-btn';
            if (idx === curEye) btn.classList.add('active');
            btn.textContent = eyeColorNames[idx];
            btn.addEventListener('click', function() {
                curEye = idx;
                if (currentAppearance) currentAppearance.eyeColor = idx;
                grid.querySelectorAll('.cust-eye-btn').forEach(function(b) { b.classList.remove('active'); });
                btn.classList.add('active');
                fetch('https://gamemodecity/changeEyeColor', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ color: idx })
                });
            });
            grid.appendChild(btn);
        })(i);
    }
    container.appendChild(grid);
}

// ---- Prev/Next Helper ----
function createPrevNext(initial, max, onChange, minVal) {
    var val = initial;
    var mn = minVal !== undefined ? minVal : 0;
    var mx = max;

    var wrap = document.createElement('div');
    wrap.className = 'cust-prevnext';

    var prevBtn = document.createElement('button');
    prevBtn.className = 'cust-prevnext-btn';
    prevBtn.innerHTML = '&#9664;';

    var valSpan = document.createElement('span');
    valSpan.className = 'cust-prevnext-value';
    valSpan.textContent = val < mn ? 'None' : val + '/' + (mx - (mn < 0 ? 0 : 1));

    var nextBtn = document.createElement('button');
    nextBtn.className = 'cust-prevnext-btn';
    nextBtn.innerHTML = '&#9654;';

    function update() {
        valSpan.textContent = val < 0 ? 'None' : val + '/' + (mx - 1);
        if (onChange) onChange(val);
    }

    prevBtn.addEventListener('click', function() {
        val--;
        if (val < mn) val = mx - 1;
        update();
    });

    nextBtn.addEventListener('click', function() {
        val++;
        if (val >= mx) val = mn;
        update();
    });

    wrap.appendChild(prevBtn);
    wrap.appendChild(valSpan);
    wrap.appendChild(nextBtn);

    return {
        el: wrap,
        updateMax: function(newMax) {
            mx = newMax;
            if (val >= mx) { val = 0; update(); }
            valSpan.textContent = val < 0 ? 'None' : val + '/' + (mx - 1);
        }
    };
}

// Sub-tab clicks
document.getElementById('shopSubTabs').addEventListener('click', function(e) {
    var btn = e.target.closest('.shop-sub-tab');
    if (!btn || btn.classList.contains('disabled')) return;
    var section = btn.getAttribute('data-section');
    currentSection = section;
    document.querySelectorAll('.shop-sub-tab').forEach(function(b) { b.classList.remove('active'); });
    btn.classList.add('active');
    autoCameraForSection(section);
    renderCurrentSection();
});

// Camera buttons
document.getElementById('shopCameraBtns').addEventListener('click', function(e) {
    var btn = e.target.closest('.cam-btn');
    if (!btn) return;
    setCamera(btn.getAttribute('data-preset'));
});

// Rotate buttons
document.getElementById('rotateLeft').addEventListener('click', function() {
    fetch('https://gamemodecity/rotatePed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ delta: -15 })
    });
});

document.getElementById('rotateRight').addEventListener('click', function() {
    fetch('https://gamemodecity/rotatePed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ delta: 15 })
    });
});

// Save/Cancel buttons
document.getElementById('shopSaveBtn').addEventListener('click', function() {
    exitCustomization(false);
    switchTab('home');
});

document.getElementById('shopCancelBtn').addEventListener('click', function() {
    exitCustomization(true);
    switchTab('home');
});
