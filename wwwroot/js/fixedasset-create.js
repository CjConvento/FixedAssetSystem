// fixedasset-create.js
$(document).ready(function () {
    let rowIndex = window.initialExistingUnitCount || 0; // Set from server if validation failed

    function addRow(itemNo = '', description = '', location = '', userName = '', remarks = '') {
        const newRow = `
            <tr>
                <td><input type="number" name="ExistingUnits[${rowIndex}].ItemNo" class="form-control item-no" value="${escapeHtml(itemNo)}" /></td>
                <td><input type="text" name="ExistingUnits[${rowIndex}].Description" class="form-control description" value="${escapeHtml(description)}" required /></td>
                <td><input type="text" name="ExistingUnits[${rowIndex}].Location" class="form-control location" value="${escapeHtml(location)}" /></td>
                <td><input type="text" name="ExistingUnits[${rowIndex}].UserName" class="form-control user-name" value="${escapeHtml(userName)}" /></td>
                <td><input type="text" name="ExistingUnits[${rowIndex}].Remarks" class="form-control remarks" value="${escapeHtml(remarks)}" /></td>
                <td><button type="button" class="btn btn-danger btn-sm removeRow">Remove</button></td>
            </tr>`;
        $("#existingUnitsTable tbody").append(newRow);
        rowIndex++;
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/[&<>]/g, function (m) {
            if (m === '&') return '&amp;';
            if (m === '<') return '&lt;';
            if (m === '>') return '&gt;';
            return m;
        }).replace(/["']/g, function (m) {
            if (m === '"') return '&quot;';
            if (m === "'") return '&#39;';
            return m;
        });
    }

    function reindexRows() {
        $("#existingUnitsTable tbody tr").each(function (idx) {
            $(this).find(".item-no").val(idx + 1);
            $(this).find(".item-no").attr("name", `ExistingUnits[${idx}].ItemNo`);
            $(this).find(".description").attr("name", `ExistingUnits[${idx}].Description`);
            $(this).find(".location").attr("name", `ExistingUnits[${idx}].Location`);
            $(this).find(".user-name").attr("name", `ExistingUnits[${idx}].UserName`);
            $(this).find(".remarks").attr("name", `ExistingUnits[${idx}].Remarks`);
        });
        rowIndex = $("#existingUnitsTable tbody tr").length;
    }

    // Show/hide existing units section based on RequestType
    $("#RequestType").change(function () {
        if ($(this).val() === "Additional") {
            $("#existingUnitsSection").show();
            if ($("#existingUnitsTable tbody tr").length === 0) {
                addRow();
            }
        } else {
            $("#existingUnitsSection").hide();
            $("#existingUnitsTable tbody").empty();
            rowIndex = 0;
        }
    });

    // Add row button
    $("#addUnitRow").click(function () {
        let lastRow = $("#existingUnitsTable tbody tr:last");
        let lastDesc = lastRow.find(".description").val();
        if (lastDesc && lastDesc.trim() !== "") {
            addRow();
        } else {
            alert("Please fill in the Description for the current row before adding another.");
        }
    });

    // Remove row
    $(document).on("click", ".removeRow", function () {
        $(this).closest("tr").remove();
        reindexRows();
    });

    // Initialize: if request type is Additional, show section and ensure rows are indexed
    if ($("#RequestType").val() === "Additional") {
        $("#existingUnitsSection").show();
        // If there are existing rows (from server), reindex them
        if ($("#existingUnitsTable tbody tr").length > 0) {
            reindexRows();
        } else {
            // Optionally add an empty row if none exist (but server might have sent none)
            // We'll follow the original behavior: if no rows, add one.
            if ($("#existingUnitsTable tbody tr").length === 0) {
                addRow();
            }
        }
    } else {
        $("#existingUnitsSection").hide();
    }

    // Form submit handler (existing validation only)
    $("form").submit(function (e) {
        if ($("#RequestType").val() === "Additional") {
            let isValid = true;
            $("#existingUnitsTable tbody tr").each(function () {
                let desc = $(this).find(".description").val();
                if (!desc || desc.trim() === "") {
                    isValid = false;
                    $(this).find(".description").addClass("is-invalid");
                } else {
                    $(this).find(".description").removeClass("is-invalid");
                }
            });
            if (!isValid) {
                e.preventDefault();
                alert("Please fill in the Description for all existing unit rows. Remove any empty rows.");
                return false;
            }
        }
    });
});