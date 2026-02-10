(function() {
    var overlay = document.getElementById('br-inventory');
    var slotsContainer = document.getElementById('brInvSlots');
    var consumablesContainer = document.getElementById('brInvConsumables');
    var slotLabels = ['Gun 1', 'Gun 2', 'Melee (Locked)'];
    var currentData = null;

    function buildSlots(data) {
        slotsContainer.innerHTML = '';
        for (var i = 0; i < 3; i++) {
            var slot = data.slots[i];
            var div = document.createElement('div');
            div.className = 'br-inv-slot';
            if (i === data.activeSlot) div.classList.add('active');
            if (i === 2) div.classList.add('locked');

            var label = document.createElement('div');
            label.className = 'br-inv-slot-label';
            label.textContent = slotLabels[i];
            div.appendChild(label);

            var name = document.createElement('div');
            name.className = 'br-inv-slot-name';
            if (slot.hash === 0) {
                name.className += ' br-inv-slot-empty';
                name.textContent = 'Empty';
            } else {
                name.textContent = slot.name;
            }
            div.appendChild(name);

            if (i < 2 && slot.hash !== 0) {
                var ammo = document.createElement('div');
                ammo.className = 'br-inv-slot-ammo';
                ammo.textContent = slot.clip + ' / ' + slot.reserve;
                div.appendChild(ammo);
            }

            if (i === 2) {
                var lock = document.createElement('span');
                lock.className = 'lock-icon';
                lock.textContent = '\uD83D\uDD12';
                div.appendChild(lock);
            }

            // Drag-drop for gun slots only
            if (i < 2 && slot.hash !== 0) {
                div.draggable = true;
                div.dataset.slot = i;
                div.addEventListener('dragstart', onDragStart);
                div.addEventListener('dragend', onDragEnd);
            }

            slotsContainer.appendChild(div);
        }
    }

    function buildConsumables(data) {
        consumablesContainer.innerHTML = '';

        // Bandage
        var bDiv = document.createElement('div');
        bDiv.className = 'br-inv-consumable bandage';
        bDiv.innerHTML = '<div class="br-inv-consumable-name">Bandage</div>'
            + '<div class="br-inv-consumable-count">' + data.bandages + '</div>';
        var bBtn = document.createElement('button');
        bBtn.className = 'br-inv-use-btn';
        bBtn.textContent = 'Use (+50 HP)';
        bBtn.disabled = data.bandages <= 0;
        bBtn.addEventListener('click', function() {
            fetch('https://gta_gameroo/brUseConsumable', {
                method: 'POST',
                body: JSON.stringify({ type: 'bandage' })
            });
        });
        bDiv.appendChild(bBtn);
        consumablesContainer.appendChild(bDiv);

        // Adrenaline
        var aDiv = document.createElement('div');
        aDiv.className = 'br-inv-consumable adrenaline';
        aDiv.innerHTML = '<div class="br-inv-consumable-name">Adrenaline</div>'
            + '<div class="br-inv-consumable-count">' + data.adrenaline + '</div>';
        var aBtn = document.createElement('button');
        aBtn.className = 'br-inv-use-btn';
        aBtn.textContent = 'Use (Speed 10s)';
        aBtn.disabled = data.adrenaline <= 0;
        aBtn.addEventListener('click', function() {
            fetch('https://gta_gameroo/brUseConsumable', {
                method: 'POST',
                body: JSON.stringify({ type: 'adrenaline' })
            });
        });
        aDiv.appendChild(aBtn);
        consumablesContainer.appendChild(aDiv);
    }

    function onDragStart(e) {
        e.target.classList.add('dragging');
        e.dataTransfer.setData('text/plain', e.target.dataset.slot);
        e.dataTransfer.effectAllowed = 'move';
    }

    function onDragEnd(e) {
        e.target.classList.remove('dragging');
        // If dropped outside the inventory slots area, drop the weapon
        var rect = overlay.getBoundingClientRect();
        var slotsRect = slotsContainer.getBoundingClientRect();
        var x = e.clientX;
        var y = e.clientY;
        // If dropped outside the slots container, treat as drop
        if (x < slotsRect.left || x > slotsRect.right || y < slotsRect.top || y > slotsRect.bottom) {
            var slot = parseInt(e.target.dataset.slot);
            fetch('https://gta_gameroo/brDropWeapon', {
                method: 'POST',
                body: JSON.stringify({ slot: slot })
            });
        }
    }

    function closeInventory() {
        overlay.classList.remove('active');
        fetch('https://gta_gameroo/brCloseInventory', {
            method: 'POST',
            body: JSON.stringify({})
        });
    }

    // Keydown: Tab or Escape to close
    document.addEventListener('keydown', function(e) {
        if (!overlay.classList.contains('active')) return;
        if (e.key === 'Tab' || e.key === 'Escape') {
            e.preventDefault();
            closeInventory();
        }
    });

    // Listen for NUI messages
    window.addEventListener('message', function(event) {
        var data = event.data;
        if (data.type === 'brOpenInventory') {
            currentData = data;
            buildSlots(data);
            buildConsumables(data);
            overlay.classList.add('active');
        }
        else if (data.type === 'brCloseInventory') {
            overlay.classList.remove('active');
        }
    });
})();
