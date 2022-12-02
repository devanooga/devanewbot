<template>
    <div id="wrapper">
        <div class="main">
            <RouterView v-slot="{ Component }">
                <template v-if="Component">
                    <Suspense>
                        <component :is="Component"></component>

                        <template #fallback> Loading... </template>
                    </Suspense>
                </template>
            </RouterView>
        </div>
    </div>
</template>

<style lang="scss"></style>
