<script>

import { Page } from '@bexis2/bexis2-core-ui';
import { Api } from '@bexis2/bexis2-core-ui';
import { Spinner, positionType } from '@bexis2/bexis2-core-ui';
import {countPlots} from '../../services/PlotCountCaller';


let files;
let plotsid = [];
let result;
let fileUpload = false;
let header = false;
let run = false;
let fileError = "";

let textareaPlots ="";

	const handleSubmit = () => {

		readFile(files[0]);

	}

	async function count()
	{
		run = true;
		result="";
		
		//if header remove first line
		if(header)
		{
			var lines = textareaPlots.split('\n');
			lines.filter(line => line.trim() !== '');
			lines.splice(0,1);
			let newText = lines.join('\n');
			plotsid = newText.split(/[\r\n,\t\s;]+/);

		}
		else
		{
			plotsid = textareaPlots.split(/[\r\n,\t\s;]+/).filter(line => line.trim() !== '');
		}
		//send to bexis textareaPlots
		const respone = await countPlots(plotsid);
		console.log(respone);
		result = respone;
		
	}

	function clear()
	{
		textareaPlots = "";
		result ="";
		run = false;

	}

	function readFile(file)
	{
		if(file.type == "text/plain" || file.type == "text/csv")
		{
		fileError = "";
		const reader = new FileReader();
		const input = file;
		let json;

		reader.readAsText(input);

		reader.onload = function (e) {
		//debugger;	
		if (!e.target?.result) return;
		else
		{
        const text = e.target.result;
		console.log("text",text);
		textareaPlots = text.toString();
		}

      };
	}
	else
	{
		fileError = "File format is not supported. Please use .txt or .csv.";
	}

	}

</script>

<Page title="Plot Count">

	<main>

		<div class="p-5">
	
		<div class="boxOuter">
	
		<p class="dtm-para_green">Count and check plot IDs</p>

		<p class="p-4">Enter plot IDs or upload a file. Allowed separators: comma, semicolon, space characters and enter. Also in combination.</p>
		<div class="boxLeft"><b>Plots:</b><br>
		<textarea  bind:value={textareaPlots}></textarea><br>
			
	
		<input type="checkbox" bind:checked={fileUpload}/> <b>File upload (.csv, .txt)</b>
		
		</div>
		{#if fileUpload == true}
		<div class="boxLeft">
			<input type="checkbox" bind:checked={header}/> File header exists
			<form on:submit|preventDefault={handleSubmit}>
				<input type="file" bind:files>
				<input type="submit" value="Submit" class="btn variant-filled-secondary"/> <p style="color: red; width: 400px;">{fileError}</p>
			</form>
			</div>	{/if}
		
		<hr class="dtm-para_green"/>
		<div class="grid grid-cols-2 gap-4 p-5">
		{#if textareaPlots}
			<button class="btn variant-filled-primary" on:click={count}>
				Run
			</button>
			<button class="btn variant-filled-primary" on:click={clear}>
			Clear
			</button>	
		{:else}
			<button class="btn variant-filled-primary bx-disabled" on:click={count}>
				Run
			</button>
			<button class="btn variant-filled-primary bx-disabled" on:click={clear}>
			Clear
			</button>		
		{/if}

		<p class="errors"></p>
		</div>
		</div>
	
	
		{#if result}
	
		<div class="boxOuter" id="results">
		
		<p class="dtm-para_green">Result</p>
		<ul class="p-4">
			<li><b>Number of plots</b></li>
		{#each result.plotProfiling.plotTypeCounters as item, i}
		
			<p class="resultList">Number of {item.plotType}: {item.number}</p>
	
		{/each}
		<li ><b>Joint Experiment 2020</b>	</li>
			<p class="resultList">Forest: {#if result.plotProfiling.jointExperimentForest == true}
				 yes
			{:else}
				 no
			{/if}
				
			</p>
			<p class="resultList">Grassland: {#if result.plotProfiling.jointExperimentGrld == true}
				 yes
			{:else}
				 no
			{/if}
			</p>
	
		
		<li><b>Further details</b></li>
		<p class="resultList">Number of entered plots: {result.numberOfAllPlots}</p>
		<p class="resultList">Number of duplicate plots: {result.numberOfDuplicates}</p>
		<p class="resultList-overflow">Non-valid plots:
	
		{#if result.notVaildPlotIds.length > 0}
	
			{#each result.notVaildPlotIds as item, i}
				{item}{#if i < (result.notVaildPlotIds.length-1)},{/if}
		
				{/each}
	
		{:else}
				none
	
		{/if}
		</p>
		</ul>
		</div>
	
		{:else}
		
		{#if run == true}
			<div class="spinnerBox">
				<Spinner label="counting plots" />
			</div>	
		{/if}
			
		{/if}
	
		
		
		</div>
	</main>

</Page>

<style>

	main {
		text-align: left;
		padding: 1em;
		max-width: 240px;
		margin: 0 auto;
	}

	textarea { 
		width: 100%;
		height: 200px;

	}



	.boxLeft{
	 
     padding: 20px;
     background: #FFF;
	 /* width: 40%; */
	}

	.boxOuter {
     float: left;
	 width: 40%;
	 /* height: 550px; */
	 border-left: #388670 6px solid;
	 border-right: #388670 2px solid;
     border-bottom: #388670 2px solid;
     border-top: #388670 10px solid;
     margin-bottom: 1em;
	 margin-right: 1em;
	}

	 .spinnerBox
	 {
		float: left;
	 	width: 30%;
		margin-bottom: 1em;
	 	margin-right: 1em;
	 }


.dtm-para_green 
{
    background-color: #bbddd9;
    font-size: 16px;
	font-weight: bold;
    padding: 0.5em;
	margin-block-start: 0;
	border: 0;
}

.resultList
{
	margin-left: 100px;	
}

.resultList-overflow
{
	margin-left: 100px;
	margin-right: 100px;
	height:60px;
    overflow-y : scroll;

}

	@media (min-width: 640px) 
	{
		main {
			max-width: none;
		}
	}
</style>