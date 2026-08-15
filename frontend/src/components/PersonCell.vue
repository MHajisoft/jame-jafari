<script setup>
import { computed } from 'vue'
import EntityAvatar from './EntityAvatar.vue'
import NickBadge from './NickBadge.vue'
import PersonLifeStatus from './PersonLifeStatus.vue'

const props = defineProps({
  /** PersonSummaryResponse, PersonResponse, or lookup row */
  person: { type: Object, default: null },
  displayName: { type: String, default: '' },
  nickName: { type: String, default: '' },
  picturePath: { type: String, default: '' },
  isDead: { type: Boolean, default: false },
  previewable: { type: Boolean, default: true },
  size: { type: [Number, String], default: 36 },
  emptyLabel: { type: String, default: '—' }
})

const resolvedName = computed(() => {
  if (props.displayName) return props.displayName
  if (!props.person) return ''
  if (props.person.displayName) return props.person.displayName
  return [props.person.firstName, props.person.lastName].filter(Boolean).join(' ')
})

const resolvedNick = computed(() => props.nickName || props.person?.nickName || '')
const resolvedPicture = computed(() => props.picturePath || props.person?.picturePath || '')
const resolvedDead = computed(() => props.isDead || !!props.person?.isDead)
const hasPerson = computed(() => !!resolvedName.value)
</script>

<template>
  <div v-if="hasPerson" class="person-cell">
    <EntityAvatar
      :src="resolvedPicture"
      :name="resolvedName"
      :deceased="resolvedDead"
      :size="size"
      :previewable="previewable"
      :preview-title="resolvedName"
    />
    <div class="person-cell-text">
      <span class="person-cell-name">
        <strong :class="{ 'name-deceased': resolvedDead }">{{ resolvedName }}</strong>
        <NickBadge :value="resolvedNick" />
        <PersonLifeStatus :is-dead="resolvedDead" />
      </span>
    </div>
  </div>
  <span v-else class="person-cell-empty">{{ emptyLabel }}</span>
</template>

<style scoped>
.person-cell {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}
.person-cell-text {
  min-width: 0;
  flex: 1;
}
.person-cell-name {
  display: inline-flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 0.35rem;
  min-width: 0;
}
.person-cell-name strong {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}
.name-deceased {
  color: color-mix(in srgb, var(--text-muted) 50%, var(--text));
}
.person-cell-empty {
  color: var(--text-muted);
}
</style>
