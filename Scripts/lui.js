!function () {

    /* XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Setup XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX */

    const $dataEl = $('#divQuery0')
    datasetId = $dataEl.data('datasetid'),
        showdataUrl = $dataEl.data('showdataurl');

    /* XXXXXXXXXXXXXXXXXXXXXXXXXX Event Handlers XXXXXXXXXXXXXXXXXXXXXXXXXXX */

    /* ------------------------------ Query 0 ------------------------------ */

    $('#divQuery0 input')
        .on('change', function () {

            // hide all further steps
            $('#divQuery1, #divQuery2, #divQuery3, #divQuery4, #btnCalculateLUI').hide();
            $("#divResultButtons").hide();

            // get selected option
            var selected = $(this).val();

            if (selected == "standardized") {
                // show next step of wizzard and remove results table
                resetQuery1();
                $('#divQuery1').show();
                $('#divResultGrid').empty();
            }
            else {
                // show complete dataset and leave wizzard steps hidden
                $('#divResultGrid').empty();
                $('#dtmLuiSpinner').show();
                $.ajax({
                    method: 'get',
                    url: showdataUrl,
                    data: {
                        datasetID: datasetId
                    }
                })
                    .done(function (data) {

                        // add data to result
                        $('#divResultGrid').html(data);

                        // disable loading spinner
                        $('#dtmLuiSpinner').hide();

                    });
            }

        });

    /* ------------------------------ Query 1 ------------------------------ */

    $('#divQuery1 input')
        .on('change', function () {

            // adapt text for Explo selection (next step) based on decision in current step
            resetQuery2();
            const selectecVal = $(this).attr('value');
            $('[data-scale]').each((ind, el) => {
                let $el = $(el);
                if ($el.data('scale') == selectecVal) {
                    $el.show();
                } else {
                    $el.hide();
                }
            });

            // show next step, but hide others
            $('#divQuery2').show()
            $('html,body').animate({
                scrollTop: $("#divQuery2").offset().top
            }, 'slow');

            $('#divQuery3, #divQuery4, #btnCalculateLUI').hide();

        });


    /* ------------------------------ Query 2 ------------------------------ */

    // "Select all" buttons
    $('[data-selectall-target]')
        .on('click', function () {
            // shortcuts
            const $this = $(this),
                $target = $($this.data('selectall-target')),
                enable = $this.hasClass('dtm-selectall');

            // switch mode
            $this.toggleClass('dtm-selectall');

            // select/unselect all item in target
            $target.find('input[type="checkbox"]')
                .prop('checked', enable)
                .trigger('change');
        });

    // show location specific messages
    $('#divQuery2_1 input[type="checkbox"]')
        .on('change', function () {
            // get state of option
            const enabled = $(this).is(':checked');

            // set state of corresponding message
            $('#dtmLocationMessages')
                .find('[data-location="' + $(this).data('location') + '"]')[enabled ? 'show' : 'hide']();
        });

    // confirm button
    $('#btnConfirmSelection')
        .on('click', function () {

            // hide error message
            $(".dtm-error").hide();

            // check for errors
            const selectedExplos = $("input[id^='Explos']:checked").length,
                selectedScale = $("input[id^='Scales']:checked").val();
            if (selectedScale == 'global') {
                if (selectedExplos < 2) {
                    $('#errExploGlob').show();
                    return;
                }
            } else {
                if (selectedExplos < 1) {
                    $('#errExploReg').show();
                    return;
                }
            }
            if ($("input[id^='Years']:checked").length < 1) {
                $('#errYears').show();
                return;
            }

            // all checks passed, proceed
            $('#divQuery2 [data-selectall-target], #btnConfirmSelection, #divQuery2 label')
                .prop('disabled', true)
                .addClass('bx-disabled');

            $("#btnEditSelection")
                .prop('disabled', false)
                .removeClass('bx-disabled')

            // reset further querys and show the correct one
            resetQuery3();
            resetQuery4();
            if ($("input[id^='Years']:checked").length == 1) {
                $('#divQuery3').hide();
                $('#divQuery4').show()
                $('html,body').animate({
                    scrollTop: $("#divQuery4").offset().top
                }, 'slow');
            } else {
                $('#divQuery3').show()
                $('html,body').animate({
                    scrollTop: $("#divQuery3").offset().top
                }, 'slow');
                $('#divQuery4').hide();
            }
        })

    // edit button
    $('#btnEditSelection')
        .on('click', function () {

            // hide further steps
            $('#divQuery3, #divQuery4, #btnCalculateLUI').hide();

            // change active state for buttons
            $("#btnEditSelection")
                .prop('disabled', true)
                .addClass('bx-disabled')
            $('#divQuery2 [data-selectall-target], #btnConfirmSelection, #divQuery2 label')
                .prop('disabled', false)
                .removeClass('bx-disabled');

            // make sure no result is shown
            $('#divResult').hide();
            $('#divResultButtons').hide();
            $('#divResultGrid').empty();

        });

    /* ------------------------------ Query 3 ------------------------------ */

    $('#divQuery3 input')
        .on('change', function () {

            // show next step
            resetQuery4();
            $('#divQuery4').show()
            $('html,body').animate({
                scrollTop: $("#divQuery4").offset().top
            }, 'slow');

            // hide results and calculate button
            $("#btnCalculateLUI").hide();
            $('#divResultButtons').hide();
            $('#divResultGrid').empty();

        });

    /* ------------------------------ Query 4 ------------------------------ */

    $('#divQuery4 input')
        .on('change', function () {

            // hide results
            $('#divResultButtons').hide();
            $('#divResultGrid').empty();

            // show calculate button
            $("#btnCalculateLUI").show();
            $('html,body').animate({
                scrollTop: $("#btnCalculateLUI").offset().top
            }, 'slow');;

        });

    /* ------------------------------ Download Buttons ------------------------------ */

    $('.dtm-download').on('click', function () {

        // get the needed URLs
        const $container = $('#divResultButtons'),
            prepUrl = $container.data('prepurl'),
            dlUrl = $container.data('downloadurl');

        // get requested file type
        const mimeType = $(this).data('download');

        // trigger download process
        $.ajax({
            url: prepUrl,
            contentType: 'application/json; charset=utf-8',
            datatype: 'json',
            type: "GET",
            data: { mimeType }
        })
            .done(function (d) {
                $("#preloaderContainer").removePreloader();
                window.location = dlUrl + '?mimeType=' + encodeURIComponent(mimeType);
            })
            .fail(function (e) {
                console.log(e.responseText);
            });

    });

    /* XXXXXXXXXXXXXXXXXXXXXXXXXXXX Reset Steps XXXXXXXXXXXXXXXXXXXXXXXXXXXX */

    function resetQuery1() {
        $('#divQuery1 input[type="radio"]')
            .prop('checked', false);
    }

    function resetQuery2() {
        $(".dtm-error").hide();
        $('#divQuery2 input[type="checkbox"]')
            .prop('checked', false);
        $('#divQuery2 .dtm-error').hide();
        $('#dicQuery2 [data-scale]').hide();
        $('#dicQuery2 [data-scale="global"]').show();
        $("#btnSelectAllYears")
            .attr('disabled', false)
            .text("Select all");
        $('#divQuery2 .dtm-controls').css({
            'opacity': 1,
            'pointer-events': 'auto'
        });
        $('#dtmLocationMessages [data-location]').hide();
        $('[data-selectall-target]').addClass('dtm-selectall');
        $('#btnEditSelection').trigger('click');
    }

    function resetQuery3() {
        $('#divQuery3 input').prop('checked', false);
    }

    function resetQuery4() {
        $('#divQuery4 input').prop('checked', false);
    }

}();